using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace FoodieMatch.Shared.Pooling
{
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _poolRoot;
        private readonly Action<T> _prepareForUse;
        private readonly Action<T> _prepareForPool;
        private readonly ObjectPool<T> _pool;

        public ComponentPool(
            T prefab,
            Transform poolRoot,
            int prewarmCount,
            int maxRetainedCount,
            Action<T> prepareForUse = null,
            Action<T> prepareForPool = null)
        {
            _prefab = prefab;
            _poolRoot = poolRoot;
            _prepareForUse = prepareForUse;
            _prepareForPool = prepareForPool;
            _pool = new ObjectPool<T>(
                createFunc: CreateInstance,
                actionOnGet: null,
                actionOnRelease: PrepareForRelease,
                actionOnDestroy: DestroyInstance,
                collectionCheck: Debug.isDebugBuild,
                defaultCapacity: prewarmCount,
                maxSize: maxRetainedCount);

            Prewarm(prewarmCount);
        }

        public T Get(Transform parent)
        {
            T instance = _pool.Get();
            instance.transform.SetParent(parent, worldPositionStays: false);
            _prepareForUse?.Invoke(instance);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public T Get(
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            T instance = _pool.Get();
            instance.transform.SetParent(parent, worldPositionStays: false);
            _prepareForUse?.Invoke(instance);
            instance.transform.SetPositionAndRotation(
                worldPosition,
                worldRotation);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            _pool.Release(instance);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(_prefab, _poolRoot);
            instance.name = _prefab.name;
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void PrepareForRelease(T instance)
        {
            _prepareForPool?.Invoke(instance);
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(
                _poolRoot,
                worldPositionStays: false);
        }

        private static void DestroyInstance(T instance)
        {
            Object.Destroy(instance.gameObject);
        }

        private void Prewarm(int prewarmCount)
        {
            List<T> instances = new(prewarmCount);

            for (int i = 0; i < prewarmCount; i++)
            {
                instances.Add(_pool.Get());
            }

            for (int i = 0; i < instances.Count; i++)
            {
                _pool.Release(instances[i]);
            }
        }
    }
}
