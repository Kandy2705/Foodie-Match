using System;

namespace FoodieMatch.Core.Application.GameState
{
    public sealed class LevelProgressModel
    {
        public LevelProgressModel(int totalCount)
        {
            if (totalCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount));
            }

            TotalCount = totalCount;
        }

        public int ServedCount { get; private set; }
        public int TotalCount { get; }
        public int RemainingCount => TotalCount - ServedCount;
        public float ProgressRatio => (float)ServedCount / TotalCount;
        public bool IsComplete => ServedCount >= TotalCount;

        public bool TryServeFood()
        {
            if (IsComplete)
            {
                return false;
            }

            ServedCount++;
            return true;
        }
    }
}
