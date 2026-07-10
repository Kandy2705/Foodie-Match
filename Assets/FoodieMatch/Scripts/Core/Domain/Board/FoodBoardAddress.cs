using System;

namespace FoodieMatch.Core.Domain.Board
{
    public readonly struct FoodBoardAddress : IEquatable<FoodBoardAddress>
    {
        public FoodBoardAddress(
            int grillPositionIndex,
            int foodSlotIndex)
        {
            GrillPositionIndex = grillPositionIndex;
            FoodSlotIndex = foodSlotIndex;
        }

        public int GrillPositionIndex { get; }
        public int FoodSlotIndex { get; }

        public bool IsValid =>
            GrillPositionIndex >= 0 &&
            FoodSlotIndex >= 0;

        public bool Equals(FoodBoardAddress other)
        {
            return GrillPositionIndex == other.GrillPositionIndex &&
                   FoodSlotIndex == other.FoodSlotIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is FoodBoardAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (GrillPositionIndex * 397) ^ FoodSlotIndex;
            }
        }
    }
}
