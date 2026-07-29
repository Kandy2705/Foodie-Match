using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class TrayViewPool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private TrayView _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 40;
        [SerializeField, Min(1)] private int _maxRetainedCount = 80;

        private ComponentPool<TrayView> _pool;

        public void Initialize()
        {
            _pool = new ComponentPool<TrayView>(
                _prefab,
                transform,
                _prewarmCount,
                _maxRetainedCount,
                prepareForUse: PrepareForUse,
                prepareForPool: PrepareForPool);
        }

        public TrayView Get(Transform parent)
        {
            return _pool.Get(parent);
        }

        public void Release(TrayView trayView)
        {
            _pool.Release(trayView);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        private static void PrepareForPool(TrayView trayView)
        {
            trayView.ResetForPool();
        }

        private static void PrepareForUse(TrayView trayView)
        {
            trayView.ResetForUse();
        }
    }
}
