using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FoodieMatch.UI.AddressableAssets
{
    public sealed class AddressableUiFactory : IAddressableUiFactory
    {
        private sealed class InstanceRecord
        {
            public InstanceRecord(
                GameObject instance,
                Transform parent,
                AsyncOperationHandle<GameObject> handle)
            {
                Instance = instance;
                Parent = parent;
                Handle = handle;
            }

            public GameObject Instance { get; }
            public Transform Parent { get; }
            public AsyncOperationHandle<GameObject> Handle { get; }
        }

        private readonly Dictionary<string, InstanceRecord> _instances =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, Task<GameObject>> _pendingLoads =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> _pendingParents =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> _releaseWhenLoaded =
            new(StringComparer.Ordinal);

        private bool _isShutdown;

        public async Task<T> GetOrCreateAsync<T>(
            string address,
            Transform parent,
            CancellationToken cancellationToken = default)
            where T : Component
        {
            ValidateRequest<T>(address, parent);

            if (TryGetCached(address, out T cachedInstance))
            {
                Debug.Log($"[Addressables UI] Using cached instance: {address}");
                return cachedInstance;
            }

            Task<GameObject> loadTask = null;

            try
            {
                if (!_pendingLoads.TryGetValue(address, out loadTask))
                {
                    loadTask = LoadAndCacheAsync(address, parent);
                    _pendingLoads.Add(address, loadTask);
                    _pendingParents.Add(address, parent);
                }

                GameObject instance =
                    await AwaitWithCancellationAsync(loadTask, cancellationToken);

                T component = instance.GetComponent<T>();

                if (component != null)
                {
                    return component;
                }

                Release(address);
                throw new InvalidOperationException(
                    $"Addressable UI root does not contain {typeof(T).FullName}.");
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                string parentName = parent == null
                    ? "<null>"
                    : parent.name;
                Debug.LogError(
                    $"[Addressables UI] Failed: {address} | " +
                    $"Type: {typeof(T).FullName} | Parent: {parentName} | " +
                    $"Exception: {exception}");
                throw;
            }
            finally
            {
                if (loadTask != null &&
                    loadTask.IsCompleted &&
                    _pendingLoads.TryGetValue(
                        address,
                        out Task<GameObject> pendingTask) &&
                    ReferenceEquals(pendingTask, loadTask))
                {
                    _pendingLoads.Remove(address);
                    _pendingParents.Remove(address);
                }
            }
        }

        public bool TryGetCached<T>(string address, out T instance)
            where T : Component
        {
            if (!_instances.TryGetValue(address, out InstanceRecord record))
            {
                instance = null;
                return false;
            }

            if (record.Instance == null)
            {
                _instances.Remove(address);
                ReleaseRecord(address, record);
                instance = null;
                return false;
            }

            instance = record.Instance.GetComponent<T>();
            return instance != null;
        }

        public void Release(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            if (_instances.TryGetValue(address, out InstanceRecord record))
            {
                _instances.Remove(address);
                ReleaseRecord(address, record);
                return;
            }

            if (_pendingLoads.ContainsKey(address))
            {
                _releaseWhenLoaded.Add(address);
            }
        }

        public void ReleaseAll()
        {
            if (_isShutdown)
            {
                return;
            }

            _isShutdown = true;

            foreach (string address in _pendingLoads.Keys)
            {
                _releaseWhenLoaded.Add(address);
            }

            string[] loadedAddresses = new string[_instances.Count];
            _instances.Keys.CopyTo(loadedAddresses, 0);

            for (int i = 0; i < loadedAddresses.Length; i++)
            {
                Release(loadedAddresses[i]);
            }

            _instances.Clear();
            _pendingLoads.Clear();
            _pendingParents.Clear();
        }

        private async Task<GameObject> LoadAndCacheAsync(
            string address,
            Transform parent)
        {
            Debug.Log($"[Addressables UI] Loading: {address}");

            AsyncOperationHandle<GameObject> handle =
                Addressables.InstantiateAsync(
                    address,
                    parent,
                    instantiateInWorldSpace: false,
                    trackHandle: true);
            bool released = false;

            try
            {
                GameObject instance = await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded ||
                    instance == null)
                {
                    throw handle.OperationException ??
                        new InvalidOperationException(
                            "Addressables returned no UI instance.");
                }

                if (_isShutdown || _releaseWhenLoaded.Remove(address))
                {
                    Addressables.ReleaseInstance(handle);
                    released = true;
                    Debug.Log($"[Addressables UI] Released: {address}");
                    throw new ObjectDisposedException(
                        nameof(AddressableUiFactory),
                        "The UI request completed after its owner was released.");
                }

                if (_instances.TryGetValue(address, out InstanceRecord existing))
                {
                    Addressables.ReleaseInstance(handle);
                    released = true;
                    return existing.Instance;
                }

                _instances.Add(
                    address,
                    new InstanceRecord(instance, parent, handle));
                Debug.Log($"[Addressables UI] Loaded: {address}");
                return instance;
            }
            catch
            {
                _releaseWhenLoaded.Remove(address);

                if (!released && handle.IsValid())
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded &&
                        handle.Result != null)
                    {
                        Addressables.ReleaseInstance(handle);
                    }
                    else
                    {
                        Addressables.Release(handle);
                    }
                }

                throw;
            }
        }

        private void ValidateRequest<T>(string address, Transform parent)
            where T : Component
        {
            if (_isShutdown)
            {
                throw new ObjectDisposedException(nameof(AddressableUiFactory));
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "A UI address is required.",
                    nameof(address));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(
                    nameof(parent),
                    $"Cannot load {typeof(T).FullName} without a parent.");
            }

            if (_instances.TryGetValue(address, out InstanceRecord record) &&
                record.Instance != null &&
                record.Parent != parent)
            {
                throw new InvalidOperationException(
                    $"Addressable UI {address} is already parented under " +
                    $"{record.Parent.name}, not {parent.name}.");
            }

            if (_pendingParents.TryGetValue(
                    address,
                    out Transform pendingParent) &&
                pendingParent != parent)
            {
                throw new InvalidOperationException(
                    $"Addressable UI {address} is already loading under " +
                    $"{pendingParent.name}, not {parent.name}.");
            }
        }

        private static async Task<T> AwaitWithCancellationAsync<T>(
            Task<T> task,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return await task;
            }

            TaskCompletionSource<bool> cancellation =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            using CancellationTokenRegistration registration =
                cancellationToken.Register(
                    state =>
                        ((TaskCompletionSource<bool>)state).TrySetResult(true),
                    cancellation);

            Task completed = await Task.WhenAny(task, cancellation.Task);

            if (!ReferenceEquals(completed, task))
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return await task;
        }

        private static void ReleaseRecord(
            string address,
            InstanceRecord record)
        {
            if (record.Handle.IsValid())
            {
                Addressables.ReleaseInstance(record.Handle);
            }

            Debug.Log($"[Addressables UI] Released: {address}");
        }
    }
}
