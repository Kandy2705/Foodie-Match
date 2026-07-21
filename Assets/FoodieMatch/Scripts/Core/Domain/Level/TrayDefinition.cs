using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class TrayDefinition
    {
        private readonly ReadOnlyCollection<int> _foodTokenIds;

        public TrayDefinition(IReadOnlyList<int> foodTokenIds)
        {
            if (foodTokenIds == null)
            {
                throw new ArgumentNullException(nameof(foodTokenIds));
            }

            ValidateFoodTokenIds(foodTokenIds);

            List<int> copiedFoodTokenIds = new(foodTokenIds);
            _foodTokenIds = copiedFoodTokenIds.AsReadOnly();
        }

        public IReadOnlyList<int> FoodTokenIds => _foodTokenIds;

        private static void ValidateFoodTokenIds(IReadOnlyList<int> foodTokenIds)
        {
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
                throw new ArgumentException("Tray must contain at least one food token.", nameof(foodTokenIds));
            }
        }
    }
}
