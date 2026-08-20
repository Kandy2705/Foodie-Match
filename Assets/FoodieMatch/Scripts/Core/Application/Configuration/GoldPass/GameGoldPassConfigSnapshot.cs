using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public sealed class GameGoldPassConfigSnapshot : IGameGoldPassConfig
    {
        private readonly ReadOnlyCollection<GoldPassMilestoneDefinition> _milestones;
        private readonly ReadOnlyDictionary<int, GoldPassMilestoneDefinition>
            _milestonesByLevel;

        public GameGoldPassConfigSnapshot(
            GoldPassPurchaseDefinition purchase,
            DayOfWeek resetDayUtc,
            int resetHourUtc,
            IReadOnlyList<GoldPassMilestoneDefinition> milestones)
        {
            Purchase = purchase ??
                throw new ArgumentNullException(nameof(purchase));

            if (resetHourUtc < 0 || resetHourUtc > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(resetHourUtc));
            }

            if (milestones == null || milestones.Count == 0)
            {
                throw new ArgumentException(
                    "At least one Gold Pass milestone is required.",
                    nameof(milestones));
            }

            List<GoldPassMilestoneDefinition> copiedMilestones = new(
                milestones.Count);
            Dictionary<int, GoldPassMilestoneDefinition> milestonesByLevel = new(
                milestones.Count);
            int previousLevel = -1;
            int previousRequiredSpoons = -1;

            for (int i = 0; i < milestones.Count; i++)
            {
                GoldPassMilestoneDefinition milestone = milestones[i] ??
                    throw new ArgumentException(
                        "Gold Pass milestones cannot contain null values.",
                        nameof(milestones));

                if (i == 0 && milestone.Level != 0)
                {
                    throw new ArgumentException(
                        "Gold Pass milestone levels must start at zero.",
                        nameof(milestones));
                }

                if (i > 0 && milestone.Level <= previousLevel)
                {
                    throw new ArgumentException(
                        "Gold Pass milestone levels must increase without duplicates.",
                        nameof(milestones));
                }

                if (i == 0 && milestone.RequiredSpoons != 0)
                {
                    throw new ArgumentException(
                        "Gold Pass milestone zero must require zero spoons.",
                        nameof(milestones));
                }

                if (i > 0 && milestone.RequiredSpoons <= previousRequiredSpoons)
                {
                    throw new ArgumentException(
                        "Gold Pass required spoons must increase between milestones.",
                        nameof(milestones));
                }

                copiedMilestones.Add(milestone);
                milestonesByLevel.Add(milestone.Level, milestone);
                previousLevel = milestone.Level;
                previousRequiredSpoons = milestone.RequiredSpoons;
            }

            ResetDayUtc = resetDayUtc;
            ResetHourUtc = resetHourUtc;
            _milestones = new ReadOnlyCollection<GoldPassMilestoneDefinition>(
                copiedMilestones);
            _milestonesByLevel =
                new ReadOnlyDictionary<int, GoldPassMilestoneDefinition>(
                    milestonesByLevel);
        }

        public GoldPassPurchaseDefinition Purchase { get; }

        public DayOfWeek ResetDayUtc { get; }

        public int ResetHourUtc { get; }

        public IReadOnlyList<GoldPassMilestoneDefinition> Milestones => _milestones;

        public bool TryGetMilestone(
            int level,
            out GoldPassMilestoneDefinition milestone)
        {
            return _milestonesByLevel.TryGetValue(level, out milestone);
        }
    }
}
