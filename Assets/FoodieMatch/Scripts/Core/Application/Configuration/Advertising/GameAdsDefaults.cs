using System;

namespace FoodieMatch.Core.Application.Configuration.Advertising
{
    public static class GameAdsDefaults
    {
        private const int DefaultPostLevelAdIntervalMinutes = 5;

        public static GameAdsConfigSnapshot CreateSnapshot()
        {
            return new GameAdsConfigSnapshot(
                TimeSpan.FromMinutes(
                    DefaultPostLevelAdIntervalMinutes));
        }
    }
}
