using System;
using System.Collections.Generic;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public interface IGameGoldPassConfig
    {
        DayOfWeek ResetDayUtc { get; }

        int ResetHourUtc { get; }

        IReadOnlyList<GoldPassMilestoneDefinition> Milestones { get; }

        bool TryGetMilestone(
            int level,
            out GoldPassMilestoneDefinition milestone);
    }
}
