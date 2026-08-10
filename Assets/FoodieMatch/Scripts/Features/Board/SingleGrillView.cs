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

        public void SetupCounter(int hiddenFoodCount)
        {
            _counterView.Setup(hiddenFoodCount);
        }

        public void SetRemainingFoodCount(int hiddenFoodCount)
        {
            _counterView.SetRemainingFoodCount(hiddenFoodCount);
        }

        public override void ResetForUse()
        {
            StopIntroMotion();
            _counterView.ResetCounter();
        }

        public override void ResetForPool()
        {
            StopIntroMotion();
            _counterView.ResetCounter();
        }
    }
}
