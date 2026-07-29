using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.Features.Effects
{
    public sealed class ParticleEffectPool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private ParticleEffectView _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 4;
        [SerializeField, Min(1)] private int _maxRetainedCount = 8;

        private ComponentPool<ParticleEffectView> _pool;

        public void Initialize()
        {
            _pool = new ComponentPool<ParticleEffectView>(
                _prefab,
                transform,
                _prewarmCount,
                _maxRetainedCount,
                prepareForUse: PrepareForUse,
                prepareForPool: PrepareForPool);
        }

        public void Play(Vector3 worldPosition)
        {
            ParticleEffectView effectView = _pool.Get(
                null,
                worldPosition,
                _prefab.transform.rotation);
            effectView.Play(Release);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        private static void PrepareForUse(
            ParticleEffectView effectView)
        {
            effectView.ResetForUse();
        }

        private static void PrepareForPool(
            ParticleEffectView effectView)
        {
            effectView.ResetForPool();
        }

        private void Release(ParticleEffectView effectView)
        {
            _pool.Release(effectView);
        }
    }
}
