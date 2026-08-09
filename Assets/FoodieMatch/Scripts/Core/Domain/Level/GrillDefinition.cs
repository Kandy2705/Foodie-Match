using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class GrillDefinition
    {
        private readonly ReadOnlyCollection<int> _foodTokenIds;
        private readonly ReadOnlyCollection<TrayDefinition> _trays;

        public GrillDefinition(
            int id,
            GrillType type,
            GrillPosition position,
            IReadOnlyList<int> foodTokenIds,
            IReadOnlyList<TrayDefinition> trays)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (foodTokenIds == null)
            {
                throw new ArgumentNullException(nameof(foodTokenIds));
            }

            if (trays == null)
            {
                throw new ArgumentNullException(nameof(trays));
            }

            ValidateType(type);
            ValidateFoodTokenIds(foodTokenIds);
            ValidateTrays(trays);
            ValidateSingleGrill(type, foodTokenIds, trays);
            Id = id;
            Type = type;
            Position = position;

            List<int> copiedFoodTokenIds = new(foodTokenIds);
            List<TrayDefinition> copiedTrays = new(trays);
            _foodTokenIds = copiedFoodTokenIds.AsReadOnly();
            _trays = copiedTrays.AsReadOnly();
        }

        public int Id { get; }
        public GrillType Type { get; }
        public GrillPosition Position { get; }
        public IReadOnlyList<int> FoodTokenIds => _foodTokenIds;
        public IReadOnlyList<TrayDefinition> Trays => _trays;

        private static void ValidateType(GrillType type)
        {
            if (!Enum.IsDefined(typeof(GrillType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

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

        private static void ValidateSingleGrill(
            GrillType type,
            IReadOnlyList<int> foodTokenIds,
            IReadOnlyList<TrayDefinition> trays)
        {
            if (type != GrillType.Single)
            {
                return;
            }

            if (foodTokenIds.Count != 1)
            {
                throw new ArgumentException(
                    "Single grill must contain exactly one active food token.",
                    nameof(foodTokenIds));
            }

            for (int i = 0; i < trays.Count; i++)
            {
                if (trays[i].FoodTokenIds.Count != 1)
                {
                    throw new ArgumentException(
                        "Each single grill hidden food tray must contain exactly one food token.",
                        nameof(trays));
                }
            }
        }
    }
}
