using System;
using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.WaitingRack
{
    public sealed class WaitingRackView : MonoBehaviour
    {
        [SerializeField] private WaitingRackSlotView[] _slots;

        private Action[] _placementCompletedCallbacks;

        public int Capacity => _slots != null ? _slots.Length : 0;

        private void Awake()
        {
            ResetPlacementCompletedCallbacks();
        }

        private void OnDestroy()
        {
            Clear();
        }

        public bool SetFoodAt(int index, FoodItemView foodItemView)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.SetFood(foodItemView);
        }

        public bool TryReserveFoodAt(
            int index,
            FoodItemView foodItemView,
            out Vector3 targetPosition)
        {
            targetPosition = default;
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.TryReserveFood(
                foodItemView,
                out targetPosition);
        }

        public bool CompleteFoodPlacementAt(
            int index,
            FoodItemView expectedFoodItem)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            if (!slot.CompletePlacement(expectedFoodItem))
            {
                return false;
            }

            Action placementCompleted =
                GetPlacementCompletedCallback(index);
            SetPlacementCompletedCallback(index, null);
            placementCompleted?.Invoke();

            return true;
        }

        public bool IsPlacementComplete(int index)
        {
            WaitingRackSlotView slot = GetSlot(index);
            return slot != null && slot.IsPlacementComplete;
        }

        public void WhenPlacementCompleted(
            int index,
            Action callback)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                return;
            }

            if (slot.IsPlacementComplete)
            {
                callback?.Invoke();
                return;
            }

            if (!slot.IsReserved || callback == null)
            {
                return;
            }

            Action placementCompleted =
                GetPlacementCompletedCallback(index);
            placementCompleted -= callback;
            placementCompleted += callback;
            SetPlacementCompletedCallback(
                index,
                placementCompleted);
        }

        public FoodItemView RemoveFoodAt(int index)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return null;
            }

            return slot.RemoveFood();
        }

        public void Clear()
        {
            ResetPlacementCompletedCallbacks();

            if (_slots == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].Clear();
                }
            }
        }

        private WaitingRackSlotView GetSlot(int index)
        {
            if (_slots == null || index < 0 || index >= _slots.Length)
            {
                return null;
            }

            return _slots[index];
        }

        private Action GetPlacementCompletedCallback(int index)
        {
            EnsurePlacementCompletedCallbacks();

            return index >= 0 &&
                   index < _placementCompletedCallbacks.Length
                ? _placementCompletedCallbacks[index]
                : null;
        }

        private void SetPlacementCompletedCallback(
            int index,
            Action callback)
        {
            EnsurePlacementCompletedCallbacks();

            if (index < 0 ||
                index >= _placementCompletedCallbacks.Length)
            {
                return;
            }

            _placementCompletedCallbacks[index] = callback;
        }

        private void EnsurePlacementCompletedCallbacks()
        {
            if (_placementCompletedCallbacks == null ||
                _placementCompletedCallbacks.Length != Capacity)
            {
                _placementCompletedCallbacks = new Action[Capacity];
            }
        }

        private void ResetPlacementCompletedCallbacks()
        {
            _placementCompletedCallbacks = new Action[Capacity];
        }
    }
}
