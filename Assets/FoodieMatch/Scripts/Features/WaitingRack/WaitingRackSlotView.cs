using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.WaitingRack
{
    public sealed class WaitingRackSlotView : MonoBehaviour
    {
        [SerializeField] private Transform _foodAnchor;

        private FoodItemView _foodItemView;
        private WaitingRackSlotState _state;

        private bool IsEmpty => _state == WaitingRackSlotState.Empty;
        private bool IsReserved => _state == WaitingRackSlotState.Reserved;
        private bool IsPlacementComplete =>
            _state == WaitingRackSlotState.Occupied;

        private void OnDestroy()
        {
            Clear();
        }

        public bool RestoreFood(FoodItemView foodItemView)
        {
            if (!TryReserveFood(foodItemView, out _))
            {
                return false;
            }

            return CompletePlacement(foodItemView);
        }

        public bool TryReserveFood(
            FoodItemView foodItemView,
            out Vector3 targetPosition)
        {
            targetPosition = default;

            if (foodItemView == null)
            {
                Debug.LogWarning("Food item view is missing.", this);
                return false;
            }

            if (_foodAnchor == null)
            {
                Debug.LogWarning("Food anchor is missing.", this);
                return false;
            }

            if (!IsEmpty)
            {
                Debug.LogWarning("Waiting rack slot is already occupied.", this);
                return false;
            }

            _foodItemView = foodItemView;
            _state = WaitingRackSlotState.Reserved;
            _foodItemView.SetInteractable(false);
            targetPosition = _foodAnchor.position;

            return true;
        }

        public bool CompletePlacement(FoodItemView expectedFoodItem)
        {
            if (!IsReserved ||
                expectedFoodItem == null ||
                _foodItemView != expectedFoodItem ||
                _foodAnchor == null)
            {
                return false;
            }

            _foodItemView.transform.position = _foodAnchor.position;
            _foodItemView.SetVisualState(
                FoodItemVisualState.OnWaitingRack);
            _state = WaitingRackSlotState.Occupied;

            _foodItemView.PlayLandingFeedback();

            return true;
        }

        public FoodItemView RemoveFood()
        {
            if (!IsPlacementComplete)
            {
                return null;
            }

            FoodItemView foodItemView = _foodItemView;
            ResetSlot();

            return foodItemView;
        }

        public void Clear()
        {
            if (_foodItemView != null)
            {
                _foodItemView.CancelMotion();
            }

            ResetSlot();
        }

        private void ResetSlot()
        {
            _foodItemView = null;
            _state = WaitingRackSlotState.Empty;
        }

        private enum WaitingRackSlotState
        {
            Empty,
            Reserved,
            Occupied
        }
    }
}
