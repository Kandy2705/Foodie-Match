using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Application.GoldPass
{
    public sealed class GoldPassStatus
    {
        private readonly ReadOnlyCollection<GoldPassMilestoneStatus> _milestones;

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
        }

        public GoldPassSeasonPeriod Season { get; }

        public int SpoonCount { get; }

        public bool IsSeasonPassPurchased { get; }

        public int? NextMilestoneLevel { get; }

        public int CurrentSegmentSpoons { get; }

        public int RequiredSegmentSpoons { get; }

        public bool IsComplete => !NextMilestoneLevel.HasValue;

        public IReadOnlyList<GoldPassMilestoneStatus> Milestones => _milestones;
    }
}
