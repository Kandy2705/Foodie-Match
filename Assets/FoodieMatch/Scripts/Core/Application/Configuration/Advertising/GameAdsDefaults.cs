using System;

namespace FoodieMatch.Core.Application.Configuration.Advertising
{
    public static class GameAdsDefaults
    {
        private const int DefaultPostLevelAdStartLevel = 3;
        private const int DefaultPostLevelAdIntervalMinutes = 5;

        public static GameAdsConfigSnapshot CreateSnapshot()
        {
            return new GameAdsConfigSnapshot(
                DefaultPostLevelAdStartLevel,
                TimeSpan.FromMinutes(
                    DefaultPostLevelAdIntervalMinutes));
        }
    }
}
