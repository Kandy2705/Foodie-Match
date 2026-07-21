using System;

namespace FoodieMatch.Editor.LevelDesign
{
    public sealed class LevelSeedSolverSettings
    {
        public LevelSeedSolverSettings(
            int maximumVisitedStates,
            TimeSpan maximumDuration)
        {
            if (maximumVisitedStates <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumVisitedStates));
            }

            if (maximumDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumDuration));
            }

            MaximumVisitedStates = maximumVisitedStates;
            MaximumDuration = maximumDuration;
        }

        public int MaximumVisitedStates { get; }
        public TimeSpan MaximumDuration { get; }
    }
}
