using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Shared.Pooling;
using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class GrillViewPool : MonoBehaviour, IPoolLifecycle
    {
        [Header("Standard Grill")]
        [SerializeField] private GrillView _standardPrefab;
        [SerializeField, Min(0)] private int _standardPrewarmCount = 12;
        [SerializeField, Min(1)] private int _standardMaxRetainedCount = 30;

        [Header("Single Grill")]
        [SerializeField] private SingleGrillView _singlePrefab;
        [SerializeField, Min(0)] private int _singlePrewarmCount = 2;
        [SerializeField, Min(1)] private int _singleMaxRetainedCount = 8;

        [Header("Stacked Grill")]
        [SerializeField] private StackedGrillView _stackedPrefab;
        [SerializeField, Min(0)] private int _stackedPrewarmCount = 15;
        [SerializeField, Min(1)] private int _stackedMaxRetainedCount = 30;

        private ComponentPool<GrillView> _standardPool;
        private ComponentPool<SingleGrillView> _singlePool;
        private ComponentPool<StackedGrillView> _stackedPool;

        public void Initialize()
        {
            _standardPool = new ComponentPool<GrillView>(
                _standardPrefab,
                transform,
                _standardPrewarmCount,
                _standardMaxRetainedCount,
                prepareForPool: PrepareStandardForPool);
            _singlePool = new ComponentPool<SingleGrillView>(
                _singlePrefab,
                transform,
                _singlePrewarmCount,
                _singleMaxRetainedCount);
            _stackedPool = new ComponentPool<StackedGrillView>(
                _stackedPrefab,
                transform,
                _stackedPrewarmCount,
                _stackedMaxRetainedCount);
        }

        public GrillViewBase Get(
            GrillLayoutType layoutType,
            GrillType grillType,
            Transform parent)
        {
            if (layoutType == GrillLayoutType.StackedColumns)
            {
                return _stackedPool.Get(parent);
            }

            return grillType == GrillType.Single
                ? _singlePool.Get(parent)
                : _standardPool.Get(parent);
        }

        public void Release(GrillViewBase grillView)
        {
            switch (grillView)
            {
                case GrillView standardGrill:
                    _standardPool.Release(standardGrill);
                    break;

                case SingleGrillView singleGrill:
                    _singlePool.Release(singleGrill);
                    break;

                case StackedGrillView stackedGrill:
                    _stackedPool.Release(stackedGrill);
                    break;
            }
        }

        public void Clear()
        {
            _standardPool.Clear();
            _singlePool.Clear();
            _stackedPool.Clear();
        }

        private static void PrepareStandardForPool(GrillView grillView)
        {
            grillView.CancelMotion();
        }
    }
}
