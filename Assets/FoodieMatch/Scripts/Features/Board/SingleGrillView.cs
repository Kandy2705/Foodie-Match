using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class SingleGrillView : GrillViewBase
    {
        [SerializeField] private Transform _foodAnchor;
        [SerializeField] private SingleGrillCounterView _counterView;

        public override int FoodAnchorCount => _foodAnchor != null ? 1 : 0;

        public override Transform GetFoodAnchor(int index)
        {
            return index == 0 ? _foodAnchor : null;
        }

        public bool TrySetHiddenFoodCount(int hiddenFoodCount)
        {
            if (_foodAnchor == null)
            {
                Debug.LogError("Single grill food anchor is missing.", this);
                return false;
            }

            if (_counterView == null)
            {
                Debug.LogError("Single grill counter view is missing.", this);
                return false;
            }

            return _counterView.TrySetHiddenFoodCount(hiddenFoodCount);
        }
    }
}
