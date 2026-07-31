using System;

namespace FoodieMatch.Core.Application.Level
{
    public static class LevelSynchronizationSettings
    {
        public const int FollowingLevelCount = 6;
        public static readonly TimeSpan LoadingWaitLimit =
            TimeSpan.FromSeconds(10);
    }
}
