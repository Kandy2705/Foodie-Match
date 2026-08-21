using System;
using System.Collections.Generic;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public sealed class GameGoldPassConfigSession : IGameGoldPassConfig
    {
        private IGameGoldPassConfig _current;

        public GameGoldPassConfigSession(IGameGoldPassConfig initial)
        {
            _current = initial;
        }

        public GoldPassPurchaseDefinition Purchase => _current.Purchase;

        public DayOfWeek ResetDayUtc => _current.ResetDayUtc;

        public int ResetHourUtc => _current.ResetHourUtc;

        public IReadOnlyList<GoldPassMilestoneDefinition> Milestones =>
            _current.Milestones;

        public bool TryGetMilestone(
            int level,
            out GoldPassMilestoneDefinition milestone)
        {
            return _current.TryGetMilestone(level, out milestone);
        }

        public void Apply(IGameGoldPassConfig config)
        {
            _current = config;
        }
    }
}
