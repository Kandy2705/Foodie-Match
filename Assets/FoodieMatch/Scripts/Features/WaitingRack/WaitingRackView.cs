using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.WaitingRack
{
    public sealed class WaitingRackView : MonoBehaviour
    {
        [SerializeField] private WaitingRackSlotView[] _slots;

        public int Capacity => _slots != null ? _slots.Length : 0;
        public bool HasEmptySlot => GetFirstEmptySlotIndex() >= 0;
        public bool IsFull => Capacity > 0 && !HasEmptySlot;

        public int OccupiedCount
        {
            get
            {
                if (_slots == null)
                {
                    return 0;
                }

                int count = 0;

                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null && !_slots[i].IsEmpty)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int GetFirstEmptySlotIndex()
        {
            if (_slots == null)
            {
                return -1;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].IsEmpty)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool TryPlaceFood(FoodItemView foodItemView)
        {
            int slotIndex = GetFirstEmptySlotIndex();

            if (slotIndex < 0)
            {
                return false;
            }

            return TryPlaceFoodAt(slotIndex, foodItemView);
        }

        public bool TryPlaceFoodAt(int index, FoodItemView foodItemView)
        {
            WaitingRackSlotView slot = GetSlot(index);

            if (slot == null)
            {
                Debug.LogWarning($"Waiting rack slot {index} is missing.", this);
                return false;
            }

            return slot.TryPlaceFood(foodItemView);
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
