using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Application.Configuration.GoldPass;

namespace FoodieMatch.Core.Application.GoldPass
{
    public sealed class GoldPassStatus
    {
        private readonly ReadOnlyCollection<GoldPassMilestoneStatus> _milestones;
        private readonly ReadOnlyCollection<GoldPassRewardDefinition>
            _claimableRewards;

        public GoldPassStatus(
            GoldPassSeasonPeriod season,
            int spoonCount,
            bool isSeasonPassPurchased,
            int? nextMilestoneLevel,
            int currentSegmentSpoons,
            int requiredSegmentSpoons,
            IReadOnlyList<GoldPassMilestoneStatus> milestones)
        {
            Season = season ?? throw new ArgumentNullException(nameof(season));
            SpoonCount = spoonCount;
            IsSeasonPassPurchased = isSeasonPassPurchased;
            NextMilestoneLevel = nextMilestoneLevel;
            CurrentSegmentSpoons = currentSegmentSpoons;
            RequiredSegmentSpoons = requiredSegmentSpoons;
            _milestones = new ReadOnlyCollection<GoldPassMilestoneStatus>(
                new List<GoldPassMilestoneStatus>(milestones));

            List<GoldPassRewardDefinition> claimableRewards = new();

            for (int i = 0; i < milestones.Count; i++)
            {
                GoldPassMilestoneStatus milestone = milestones[i];

                if (!milestone.IsUnlocked)
                {
                    continue;
                }

                if (!milestone.IsFreeRewardClaimed)
                {
                    claimableRewards.Add(milestone.Definition.FreeReward);
                }

                if (isSeasonPassPurchased &&
                    !milestone.IsSeasonRewardClaimed)
                {
                    claimableRewards.Add(milestone.Definition.SeasonReward);
                }
            }

            _claimableRewards =
                new ReadOnlyCollection<GoldPassRewardDefinition>(
                    claimableRewards);
        }

        public GoldPassSeasonPeriod Season { get; }

        public int SpoonCount { get; }

        public bool IsSeasonPassPurchased { get; }

        public int? NextMilestoneLevel { get; }

        public int CurrentSegmentSpoons { get; }

        public int RequiredSegmentSpoons { get; }

        public bool IsComplete => !NextMilestoneLevel.HasValue;

        public IReadOnlyList<GoldPassMilestoneStatus> Milestones => _milestones;

        public IReadOnlyList<GoldPassRewardDefinition> ClaimableRewards =>
            _claimableRewards;

        public bool HasClaimableRewards => _claimableRewards.Count > 0;
    }
}
