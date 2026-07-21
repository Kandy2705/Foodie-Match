using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class GrillDefinition
    {
        private readonly ReadOnlyCollection<int> _foodTokenIds;
        private readonly ReadOnlyCollection<TrayDefinition> _trays;

        public GrillDefinition(
            GrillPosition position,
            IReadOnlyList<int> foodTokenIds,
            IReadOnlyList<TrayDefinition> trays)
        {
            if (foodTokenIds == null)
            {
                throw new ArgumentNullException(nameof(foodTokenIds));
            }

            if (trays == null)
            {
                throw new ArgumentNullException(nameof(trays));
            }

            ValidateFoodTokenIds(foodTokenIds);
            ValidateTrays(trays);
            Position = position;

            List<int> copiedFoodTokenIds = new(foodTokenIds);
            List<TrayDefinition> copiedTrays = new(trays);
            _foodTokenIds = copiedFoodTokenIds.AsReadOnly();
            _trays = copiedTrays.AsReadOnly();
        }

        public GrillPosition Position { get; }
        public IReadOnlyList<int> FoodTokenIds => _foodTokenIds;
        public IReadOnlyList<TrayDefinition> Trays => _trays;

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
                throw new ArgumentException("Grill must contain at least one food token.", nameof(foodTokenIds));
            }
        }

        private static void ValidateTrays(IReadOnlyList<TrayDefinition> trays)
        {
            for (int i = 0; i < trays.Count; i++)
            {
                if (trays[i] == null)
                {
                    throw new ArgumentException("Tray collection cannot contain null.", nameof(trays));
                }
            }
        }
    }
}
