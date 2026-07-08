using FoodieMatch.Features.Food;
using UnityEngine;

namespace FoodieMatch.Features.WaitingRack
{
    public sealed class WaitingRackView : MonoBehaviour
    {
        [SerializeField] private WaitingRackSlotView[] _slots;

        public int Capacity => _slots != null ? _slots.Length : 0;

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
