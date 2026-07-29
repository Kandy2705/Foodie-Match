using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.Features.Effects
{
    public sealed class ParticleEffectPool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private ParticleSystem _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 4;
        [SerializeField, Min(1)] private int _maxRetainedCount = 8;

        private ComponentPool<ParticleSystem> _pool;

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

        public ParticleSystem Get(
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            ParticleSystem particleSystem = _pool.Get(
                null,
                worldPosition,
                worldRotation);
            particleSystem.Play();
            return particleSystem;
        }

        public void Release(ParticleSystem particleSystem)
        {
            _pool.Release(particleSystem);
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
