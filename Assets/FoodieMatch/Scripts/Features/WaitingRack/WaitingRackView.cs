using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.WaitingRack
{
    public sealed class WaitingRackView : MonoBehaviour
    {
        [SerializeField] private WaitingRackSlotView[] _slots;

        public int Capacity => _slots != null ? _slots.Length : 0;

        private void OnDestroy()
        {
            Clear();
        }

        public bool RestoreFoodAt(int index, FoodItemView foodItemView)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.RestoreFood(foodItemView);
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

            return slot.CompletePlacement(expectedFoodItem);
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
    }
}
