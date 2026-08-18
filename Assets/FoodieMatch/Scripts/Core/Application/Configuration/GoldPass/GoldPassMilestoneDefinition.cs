using System;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public sealed class GoldPassMilestoneDefinition
    {
        public GoldPassMilestoneDefinition(
            int level,
            int requiredSpoons,
            GoldPassRewardDefinition freeReward,
            GoldPassRewardDefinition seasonReward)
        {
            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (requiredSpoons < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredSpoons));
            }

            Level = level;
            RequiredSpoons = requiredSpoons;
            FreeReward = freeReward ??
                throw new ArgumentNullException(nameof(freeReward));
            SeasonReward = seasonReward ??
                throw new ArgumentNullException(nameof(seasonReward));
        }

        public int Level { get; }

        public int RequiredSpoons { get; }

        public GoldPassRewardDefinition FreeReward { get; }

        public GoldPassRewardDefinition SeasonReward { get; }
    }
}
