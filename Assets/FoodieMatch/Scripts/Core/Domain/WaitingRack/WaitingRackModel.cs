using System;
using System.Collections.Generic;

namespace FoodieMatch.Core.Domain.WaitingRack
{
    public sealed class WaitingRackModel
    {
        public const int MaxCapacity = 7;

        private int[] _foodTokenIds;

        public WaitingRackModel(int capacity)
        {
            int validCapacity = capacity > 0 ? capacity : 0;

            if (validCapacity > MaxCapacity)
            {
                validCapacity = MaxCapacity;
            }

            _foodTokenIds = new int[validCapacity];
        }

        public int Capacity => _foodTokenIds.Length;
        public bool HasEmptySlot => GetFirstEmptySlotIndex() >= 0;
        public bool IsFull => Capacity > 0 && !HasEmptySlot;
        public bool CanExpand => Capacity < MaxCapacity;

        public bool TryExpandBy(int amount)
        {
            if (amount <= 0 || Capacity >= MaxCapacity)
            {
                return false;
            }

            int newCapacity = _foodTokenIds.Length + amount;

            if (newCapacity > MaxCapacity)
            {
                newCapacity = MaxCapacity;
            }

            if (newCapacity <= _foodTokenIds.Length)
            {
                return false;
            }

            int[] expanded = new int[newCapacity];
            Array.Copy(_foodTokenIds, expanded, _foodTokenIds.Length);
            _foodTokenIds = expanded;
            return true;
        }

        public int OccupiedCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _foodTokenIds.Length; i++)
                {
                    if (_foodTokenIds[i] > 0)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int GetFoodTokenIdAt(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return 0;
            }

            return _foodTokenIds[slotIndex];
        }

        public int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < _foodTokenIds.Length; i++)
            {
                if (_foodTokenIds[i] == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool CanPlace(int foodTokenId)
        {
            return foodTokenId > 0 && HasEmptySlot;
        }

        public bool TryPlaceFood(
            int foodTokenId,
            out int slotIndex)
        {
            slotIndex = -1;

            if (!CanPlace(foodTokenId))
            {
                return false;
            }

            slotIndex = GetFirstEmptySlotIndex();
            _foodTokenIds[slotIndex] = foodTokenId;

            return true;
        }

        public bool TryRemoveFoodAt(
            int slotIndex,
            out int foodTokenId)
        {
            foodTokenId = 0;

            if (!IsValidSlotIndex(slotIndex) || _foodTokenIds[slotIndex] == 0)
            {
                return false;
            }

            foodTokenId = _foodTokenIds[slotIndex];
            _foodTokenIds[slotIndex] = 0;

            return true;
        }

        public bool TryRestoreFoodAt(
            int slotIndex,
            int foodTokenId)
        {
            if (!IsValidSlotIndex(slotIndex) ||
                foodTokenId <= 0 ||
                _foodTokenIds[slotIndex] != 0)
            {
                return false;
            }

            _foodTokenIds[slotIndex] = foodTokenId;
            return true;
        }

        public IReadOnlyList<int> GetFoodTokenIds()
        {
            List<int> foodTokenIds = new List<int>();

            for (int i = 0; i < _foodTokenIds.Length; i++)
            {
                if (_foodTokenIds[i] > 0)
                {
                    foodTokenIds.Add(_foodTokenIds[i]);
                }
            }

            return foodTokenIds;
        }

        public void Clear()
        {
            for (int i = 0; i < _foodTokenIds.Length; i++)
            {
                _foodTokenIds[i] = 0;
            }
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _foodTokenIds.Length;
        }
    }
}
