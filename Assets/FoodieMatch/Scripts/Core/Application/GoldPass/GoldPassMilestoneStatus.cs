using System;
using FoodieMatch.Core.Application.Configuration.GoldPass;

namespace FoodieMatch.Core.Application.GoldPass
{
    public sealed class GoldPassMilestoneStatus
    {
        public GoldPassMilestoneStatus(
            GoldPassMilestoneDefinition definition,
            bool isUnlocked,
            bool isFreeRewardClaimed,
            bool isSeasonRewardClaimed)
        {
            Definition = definition ??
                throw new ArgumentNullException(nameof(definition));
            IsUnlocked = isUnlocked;
            IsFreeRewardClaimed = isFreeRewardClaimed;
            IsSeasonRewardClaimed = isSeasonRewardClaimed;
        }

        public GoldPassMilestoneDefinition Definition { get; }

        public bool IsUnlocked { get; }

        public bool IsFreeRewardClaimed { get; }

        public bool IsSeasonRewardClaimed { get; }
    }
}
