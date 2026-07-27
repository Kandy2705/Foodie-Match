using UnityEngine;

namespace FoodieMatch.Features.Board
{
    public sealed class SingleGrillView : GrillViewBase
    {
        [SerializeField] private Transform _foodAnchor;
        [SerializeField] private SingleGrillCounterView _counterView;

        public override int FoodAnchorCount => 1;

        public override Transform GetFoodAnchor(int index)
        {
            return index == 0 ? _foodAnchor : null;
        }

        public bool TrySetHiddenFoodCount(int hiddenFoodCount)
        {
            return _counterView.TrySetHiddenFoodCount(hiddenFoodCount);
        }
    }
}
