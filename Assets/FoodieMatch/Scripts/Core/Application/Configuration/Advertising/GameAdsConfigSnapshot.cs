using System;

namespace FoodieMatch.Core.Application.Configuration.Advertising
{
    public sealed class GameAdsConfigSnapshot : IGameAdsConfig
    {
        public GameAdsConfigSnapshot(
            int postLevelAdStartLevel,
            TimeSpan postLevelAdInterval)
        {
            if (postLevelAdStartLevel <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(postLevelAdStartLevel),
                    postLevelAdStartLevel,
                    "Post-level ad start level must be greater than zero.");
            }

            if (postLevelAdInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(postLevelAdInterval),
                    postLevelAdInterval,
                    "Post-level ad interval must be greater than zero.");
            }

            PostLevelAdStartLevel = postLevelAdStartLevel;
            PostLevelAdInterval = postLevelAdInterval;
        }

        public int PostLevelAdStartLevel { get; }

        public TimeSpan PostLevelAdInterval { get; }
    }
}
