using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.WaitingRack
{
    public sealed class WaitingRackSlotView : MonoBehaviour
    {
        [SerializeField] private Transform _foodAnchor;

        private FoodItemView _foodItemView;

        public bool IsEmpty => _foodItemView == null;
        public FoodItemView FoodItemView => _foodItemView;

        public bool TryPlaceFood(FoodItemView foodItemView)
        {
            if (foodItemView == null)
            {
                Debug.LogWarning("Food item view is missing.", this);
                return false;
            }

            if (!IsEmpty)
            {
                Debug.LogWarning("Waiting rack slot is already occupied.", this);
                return false;
            }

            if (_foodAnchor == null)
            {
                Debug.LogWarning("Food anchor is missing.", this);
                return false;
            }

            _foodItemView = foodItemView;
            _foodItemView.transform.position = _foodAnchor.position;
            _foodItemView.SetVisualState(FoodItemVisualState.OnWaitingRack);
            _foodItemView.SetInteractable(false);

            return true;
        }

        public FoodItemView RemoveFood()
        {
            var foodItemView = _foodItemView;
            _foodItemView = null;
            return foodItemView;
        }

        public void Clear()
        {
            _foodItemView = null;
        }
    }
}
