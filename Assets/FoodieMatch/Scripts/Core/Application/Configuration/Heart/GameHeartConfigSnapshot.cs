using System;

namespace FoodieMatch.Core.Application.Configuration.Heart
{
    public sealed class GameHeartConfigSnapshot : IGameHeartConfig
    {
        public GameHeartConfigSnapshot(
            int maxHeartCount,
            TimeSpan heartRecoveryDuration)
        {
            if (maxHeartCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHeartCount),
                    maxHeartCount,
                    "Maximum heart count must be greater than zero.");
            }

            if (heartRecoveryDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heartRecoveryDuration),
                    heartRecoveryDuration,
                    "Heart recovery duration must be greater than zero.");
            }

            MaxHeartCount = maxHeartCount;
            HeartRecoveryDuration = heartRecoveryDuration;
        }

        public int MaxHeartCount { get; }

        public TimeSpan HeartRecoveryDuration { get; }
    }
}
