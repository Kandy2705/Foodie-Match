using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Core.Domain.Grill
{
    public sealed class TrayModel
    {
        private readonly int[] _foodTokenIds;

        public TrayModel(IReadOnlyList<int> foodTokenIds)
        {
            ValidateFoodSlots(foodTokenIds);

            _foodTokenIds = new int[foodTokenIds.Count];

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                _foodTokenIds[i] = foodTokenIds[i];
            }
        }

        public int SlotCount => _foodTokenIds.Length;

        public int FoodCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < _foodTokenIds.Length; i++)
                {
                    if (_foodTokenIds[i] > BoardRules.EmptyFoodTokenId)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int GetFoodTokenIdAt(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _foodTokenIds.Length
                ? _foodTokenIds[slotIndex]
                : BoardRules.EmptyFoodTokenId;
        }

        public bool TryRemoveFoodAt(int slotIndex, int expectedFoodTokenId)
        {
            if (slotIndex < 0 ||
                slotIndex >= _foodTokenIds.Length ||
                _foodTokenIds[slotIndex] != expectedFoodTokenId)
            {
                return false;
            }

            _foodTokenIds[slotIndex] = BoardRules.EmptyFoodTokenId;
            return true;
        }

        private static void ValidateFoodSlots(
            IReadOnlyList<int> foodTokenIds)
        {
            if (foodTokenIds == null)
            {
                throw new ArgumentNullException(nameof(foodTokenIds));
            }

            if (foodTokenIds.Count < BoardRules.MinFoodSlotCount ||
                foodTokenIds.Count > BoardRules.MaxFoodSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(foodTokenIds));
            }

            bool hasFood = false;

            for (int i = 0; i < foodTokenIds.Count; i++)
            {
                if (foodTokenIds[i] < BoardRules.EmptyFoodTokenId)
                {
                    throw new ArgumentOutOfRangeException(nameof(foodTokenIds));
                }

                hasFood |= foodTokenIds[i] > BoardRules.EmptyFoodTokenId;
            }

            if (!hasFood)
            {
                throw new ArgumentException(
                    "Tray must contain at least one food token.",
                    nameof(foodTokenIds));
            }
        }
    }
}
