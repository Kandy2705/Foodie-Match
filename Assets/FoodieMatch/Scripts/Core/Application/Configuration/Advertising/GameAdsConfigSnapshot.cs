using System;

namespace FoodieMatch.Core.Application.Configuration.Advertising
{
    public sealed class GameAdsConfigSnapshot : IGameAdsConfig
    {
        public GameAdsConfigSnapshot(TimeSpan postLevelAdInterval)
        {
            if (postLevelAdInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(postLevelAdInterval),
                    postLevelAdInterval,
                    "Post-level ad interval must be greater than zero.");
            }

            PostLevelAdInterval = postLevelAdInterval;
        }

        public TimeSpan PostLevelAdInterval { get; }
    }
}
