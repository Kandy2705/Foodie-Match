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
                string address,
                GameObject instance,
                Transform parent,
                AsyncOperationHandle<GameObject> handle)
            {
                Address = address;
                Instance = instance;
                Parent = parent;
                Handle = handle;
            }

            public string Address { get; }
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
        private readonly Dictionary<
            string,
            AsyncOperationHandle<IList<GameObject>>> _loadedLabels =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, Task> _pendingLabelLoads =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> _releaseLabelWhenLoaded =
            new(StringComparer.Ordinal);
        private readonly Dictionary<int, float> _operationProgress = new();

        private int _nextOperationId;
        private bool _isShutdown;

        public event Action<bool> LoadingStateChanged;

        public event Action<float> LoadingProgressChanged;

        public async Task PreloadLabelAsync(
            string label,
            CancellationToken cancellationToken = default)
        {
            ValidateLabel(label);

            if (_loadedLabels.TryGetValue(
                    label,
                    out AsyncOperationHandle<IList<GameObject>> loadedHandle))
            {
                if (loadedHandle.IsValid())
                {
                    Debug.Log(
                        $"[Addressables UI] Using cached label: {label}");
                    return;
                }

                _loadedLabels.Remove(label);
            }

            if (!_pendingLabelLoads.TryGetValue(label, out Task loadTask))
            {
                loadTask = LoadAndCacheLabelAsync(label);
                _pendingLabelLoads.Add(label, loadTask);
            }

            try
            {
                await AwaitWithCancellationAsync(loadTask, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Addressables UI] Failed label: {label} | " +
                    $"Exception: {exception}");
                throw;
            }
            finally
            {
                if (loadTask.IsCompleted &&
                    _pendingLabelLoads.TryGetValue(
                        label,
                        out Task pendingTask) &&
                    ReferenceEquals(pendingTask, loadTask))
                {
                    _pendingLabelLoads.Remove(label);
                }
            }
        }

        public async Task<T> GetOrCreateAsync<T>(
            string address,
            Transform parent,
            CancellationToken cancellationToken = default)
            where T : Component
        {
            return await GetOrCreateAsync<T>(
                address,
                address,
                parent,
                cancellationToken);
        }

        public async Task<T> GetOrCreateAsync<T>(
            string address,
            string instanceKey,
            Transform parent,
            CancellationToken cancellationToken = default)
            where T : Component
        {
            ValidateRequest<T>(address, instanceKey, parent);

            if (TryGetCached(instanceKey, out T cachedInstance))
            {
                Debug.Log(
                    $"[Addressables UI] Using cached instance: {address} | " +
                    $"Key: {instanceKey}");
                return cachedInstance;
            }

            Task<GameObject> loadTask = null;

            try
            {
                if (!_pendingLoads.TryGetValue(instanceKey, out loadTask))
                {
                    loadTask = LoadAndCacheAsync(
                        address,
                        instanceKey,
                        parent);
                    _pendingLoads.Add(instanceKey, loadTask);
                    _pendingParents.Add(instanceKey, parent);
                }

                GameObject instance =
                    await AwaitWithCancellationAsync(loadTask, cancellationToken);

                T component = instance.GetComponent<T>();

                if (component != null)
                {
                    return component;
                }

                Release(instanceKey);
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
                        instanceKey,
                        out Task<GameObject> pendingTask) &&
                    ReferenceEquals(pendingTask, loadTask))
                {
                    _pendingLoads.Remove(instanceKey);
                    _pendingParents.Remove(instanceKey);
                }
            }
        }

        public bool TryGetCached<T>(string instanceKey, out T instance)
            where T : Component
        {
            if (!_instances.TryGetValue(instanceKey, out InstanceRecord record))
            {
                instance = null;
                return false;
            }

            if (record.Instance == null)
            {
                _instances.Remove(instanceKey);
                ReleaseRecord(instanceKey, record);
                instance = null;
                return false;
            }

            instance = record.Instance.GetComponent<T>();
            return instance != null;
        }

        public void Release(string instanceKey)
        {
            if (string.IsNullOrWhiteSpace(instanceKey))
            {
                return;
            }

            if (_instances.TryGetValue(instanceKey, out InstanceRecord record))
            {
                _instances.Remove(instanceKey);
                ReleaseRecord(instanceKey, record);
                return;
            }

            if (_pendingLoads.ContainsKey(instanceKey))
            {
                _releaseWhenLoaded.Add(instanceKey);
            }
        }

        public void ReleaseLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            if (_loadedLabels.TryGetValue(
                    label,
                    out AsyncOperationHandle<IList<GameObject>> handle))
            {
                _loadedLabels.Remove(label);

                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                Debug.Log($"[Addressables UI] Released label: {label}");
                return;
            }

            if (_pendingLabelLoads.ContainsKey(label))
            {
                _releaseLabelWhenLoaded.Add(label);
            }
        }

        public void ReleaseAll()
        {
            if (_isShutdown)
            {
                return;
            }

            _isShutdown = true;

            foreach (string instanceKey in _pendingLoads.Keys)
            {
                _releaseWhenLoaded.Add(instanceKey);
            }

            foreach (string label in _pendingLabelLoads.Keys)
            {
                _releaseLabelWhenLoaded.Add(label);
            }

            string[] instanceKeys = new string[_instances.Count];
            _instances.Keys.CopyTo(instanceKeys, 0);

            for (int i = 0; i < instanceKeys.Length; i++)
            {
                Release(instanceKeys[i]);
            }

            string[] loadedLabels = new string[_loadedLabels.Count];
            _loadedLabels.Keys.CopyTo(loadedLabels, 0);

            for (int i = 0; i < loadedLabels.Length; i++)
            {
                ReleaseLabel(loadedLabels[i]);
            }

            _instances.Clear();
            _pendingLoads.Clear();
            _pendingParents.Clear();
            _loadedLabels.Clear();
            _pendingLabelLoads.Clear();
        }

        private async Task LoadAndCacheLabelAsync(string label)
        {
            int operationId = BeginLoading();
            Debug.Log($"[Addressables UI] Loading label: {label}");

            AsyncOperationHandle<IList<GameObject>> handle =
                Addressables.LoadAssetsAsync<GameObject>(
                    label,
                    callback: null);
            bool released = false;

            try
            {
                await AwaitWithProgressAsync(handle, operationId);

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw handle.OperationException ??
                        new InvalidOperationException(
                            $"Addressables could not load label {label}.");
                }

                if (_isShutdown || _releaseLabelWhenLoaded.Remove(label))
                {
                    Addressables.Release(handle);
                    released = true;
                    Debug.Log($"[Addressables UI] Released label: {label}");
                    throw new ObjectDisposedException(
                        nameof(AddressableUiFactory),
                        "The label completed after its owner was released.");
                }

                if (_loadedLabels.ContainsKey(label))
                {
                    Addressables.Release(handle);
                    released = true;
                    return;
                }

                _loadedLabels.Add(label, handle);
                Debug.Log($"[Addressables UI] Loaded label: {label}");
            }
            catch
            {
                _releaseLabelWhenLoaded.Remove(label);

                if (!released && handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }
            finally
            {
                EndLoading(operationId);
            }
        }

        private async Task<GameObject> LoadAndCacheAsync(
            string address,
            string instanceKey,
            Transform parent)
        {
            int operationId = BeginLoading();

            try
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
                    GameObject instance =
                        await AwaitWithProgressAsync(handle, operationId);

                    if (handle.Status != AsyncOperationStatus.Succeeded ||
                        instance == null)
                    {
                        throw handle.OperationException ??
                            new InvalidOperationException(
                                "Addressables returned no UI instance.");
                    }

                    if (_isShutdown || _releaseWhenLoaded.Remove(instanceKey))
                    {
                        Addressables.ReleaseInstance(handle);
                        released = true;
                        Debug.Log($"[Addressables UI] Released: {address}");
                        throw new ObjectDisposedException(
                            nameof(AddressableUiFactory),
                            "The UI request completed after its owner was released.");
                    }

                    if (_instances.TryGetValue(
                            instanceKey,
                            out InstanceRecord existing))
                    {
                        Addressables.ReleaseInstance(handle);
                        released = true;
                        return existing.Instance;
                    }

                    _instances.Add(
                        instanceKey,
                        new InstanceRecord(
                            address,
                            instance,
                            parent,
                            handle));
                    Debug.Log($"[Addressables UI] Loaded: {address}");
                    return instance;
                }
                catch
                {
                    _releaseWhenLoaded.Remove(instanceKey);

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
            finally
            {
                EndLoading(operationId);
            }
        }

        private int BeginLoading()
        {
            int operationId = ++_nextOperationId;
            bool wasIdle = _operationProgress.Count == 0;
            _operationProgress.Add(operationId, 0f);

            if (wasIdle)
            {
                LoadingStateChanged?.Invoke(true);
            }

            ReportLoadingProgress();
            return operationId;
        }

        private void EndLoading(int operationId)
        {
            UpdateLoadingProgress(operationId, 1f);
            _operationProgress.Remove(operationId);

            if (_operationProgress.Count == 0)
            {
                LoadingProgressChanged?.Invoke(1f);
                LoadingStateChanged?.Invoke(false);
                return;
            }

            ReportLoadingProgress();
        }

        private void UpdateLoadingProgress(
            int operationId,
            float progress)
        {
            if (!_operationProgress.ContainsKey(operationId))
            {
                return;
            }

            _operationProgress[operationId] = Mathf.Clamp01(progress);
            ReportLoadingProgress();
        }

        private void ReportLoadingProgress()
        {
            if (_operationProgress.Count == 0)
            {
                return;
            }

            float totalProgress = 0f;

            foreach (float progress in _operationProgress.Values)
            {
                totalProgress += progress;
            }

            LoadingProgressChanged?.Invoke(
                totalProgress / _operationProgress.Count);
        }

        private void ValidateRequest<T>(
            string address,
            string instanceKey,
            Transform parent)
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

            if (string.IsNullOrWhiteSpace(instanceKey))
            {
                throw new ArgumentException(
                    "A UI instance key is required.",
                    nameof(instanceKey));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(
                    nameof(parent),
                    $"Cannot load {typeof(T).FullName} without a parent.");
            }

            if (_instances.TryGetValue(instanceKey, out InstanceRecord record) &&
                record.Instance != null &&
                (record.Parent != parent ||
                 !string.Equals(
                     record.Address,
                     address,
                     StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"Addressable UI key {instanceKey} is already assigned to " +
                    $"{record.Address} under {record.Parent.name}.");
            }

            if (_pendingParents.TryGetValue(
                    instanceKey,
                    out Transform pendingParent) &&
                pendingParent != parent)
            {
                throw new InvalidOperationException(
                    $"Addressable UI key {instanceKey} is already loading under " +
                    $"{pendingParent.name}, not {parent.name}.");
            }
        }

        private void ValidateLabel(string label)
        {
            if (_isShutdown)
            {
                throw new ObjectDisposedException(nameof(AddressableUiFactory));
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException(
                    "A UI label is required.",
                    nameof(label));
            }
        }

        private async Task<T> AwaitWithProgressAsync<T>(
            AsyncOperationHandle<T> handle,
            int operationId)
        {
            while (!handle.IsDone)
            {
                UpdateLoadingProgress(
                    operationId,
                    handle.PercentComplete);
                await Task.Yield();
            }

            UpdateLoadingProgress(operationId, 1f);
            return await handle.Task;
        }

        private static async Task AwaitWithCancellationAsync(
            Task task,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                await task;
                return;
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

            await task;
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
            string instanceKey,
            InstanceRecord record)
        {
            if (record.Handle.IsValid())
            {
                Addressables.ReleaseInstance(record.Handle);
            }

            Debug.Log(
                $"[Addressables UI] Released: {record.Address} | " +
                $"Key: {instanceKey}");
        }
    }
}
