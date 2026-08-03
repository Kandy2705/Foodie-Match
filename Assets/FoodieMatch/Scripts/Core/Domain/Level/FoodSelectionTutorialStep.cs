using System;
using FoodieMatch.Core.Domain.Board;

namespace FoodieMatch.Core.Domain.Level
{
    public readonly struct FoodSelectionTutorialStep
    {
        public FoodSelectionTutorialStep(int grillId, int foodSlotIndex)
        {
            if (grillId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(grillId));
            }

            if (foodSlotIndex < 0 || foodSlotIndex >= BoardRules.MaxFoodSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(foodSlotIndex));
            }

            GrillId = grillId;
            FoodSlotIndex = foodSlotIndex;
        }

        public int GrillId { get; }
        public int FoodSlotIndex { get; }
    }
}
