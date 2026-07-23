using System;

namespace FoodieMatch.Core.Application.Configuration.Heart
{
    public static class GameHeartDefaults
    {
        private const int DefaultMaxHeartCount = 5;
        private static readonly TimeSpan DefaultHeartRecoveryDuration =
            TimeSpan.FromMinutes(20);

        public static GameHeartConfigSnapshot CreateSnapshot()
        {
            return new GameHeartConfigSnapshot(
                DefaultMaxHeartCount,
                DefaultHeartRecoveryDuration);
        }
    }
}
