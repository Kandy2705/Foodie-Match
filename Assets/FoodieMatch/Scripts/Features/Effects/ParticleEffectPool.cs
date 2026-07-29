using System.Collections.Generic;
using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.Features.Effects
{
    public sealed class ParticleEffectPool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private ParticleSystem _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 4;
        [SerializeField, Min(1)] private int _maxRetainedCount = 8;

        private readonly List<ParticleSystem> _activeEffects = new();
        private ComponentPool<ParticleSystem> _pool;

        private void Update()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].IsAlive(withChildren: true))
                {
                    continue;
                }

                ParticleSystem particleSystem = _activeEffects[i];
                _activeEffects.RemoveAt(i);
                _pool.Release(particleSystem);
            }
        }

        public void Initialize()
        {
            _pool = new ComponentPool<ParticleSystem>(
                _prefab,
                transform,
                _prewarmCount,
                _maxRetainedCount,
                prepareForUse: PrepareForUse,
                prepareForPool: PrepareForPool);
        }

        public void Play(Vector3 worldPosition)
        {
            ParticleSystem particleSystem = _pool.Get(
                null,
                worldPosition,
                _prefab.transform.rotation);
            _activeEffects.Add(particleSystem);
            particleSystem.Play(withChildren: true);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        private static void PrepareForUse(
            ParticleSystem particleSystem)
        {
            particleSystem.Clear(
                withChildren: true);
        }

        private static void PrepareForPool(
            ParticleSystem particleSystem)
        {
            particleSystem.Stop(
                withChildren: true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
