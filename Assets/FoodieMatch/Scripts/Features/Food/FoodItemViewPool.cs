using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.Features.Food
{
    public sealed class FoodItemViewPool : MonoBehaviour, IPoolLifecycle
    {
        [SerializeField] private FoodItemView _prefab;
        [SerializeField, Min(0)] private int _prewarmCount = 80;
        [SerializeField, Min(1)] private int _maxRetainedCount = 140;

        private ComponentPool<FoodItemView> _pool;

        public void Initialize()
        {
            _pool = new ComponentPool<FoodItemView>(
                _prefab,
                transform,
                _prewarmCount,
                _maxRetainedCount,
                prepareForPool: PrepareForPool);
        }

        public FoodItemView Get(Transform parent)
        {
            return _pool.Get(parent);
        }

        public FoodItemView Get(
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            return _pool.Get(
                parent,
                worldPosition,
                worldRotation);
        }

        public void Release(FoodItemView foodItemView)
        {
            _pool.Release(foodItemView);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        private static void PrepareForPool(FoodItemView foodItemView)
        {
            foodItemView.Clear();
        }
    }
}
