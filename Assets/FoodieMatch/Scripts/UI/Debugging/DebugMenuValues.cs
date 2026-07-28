using FoodieMatch.Core.Application.Player;

namespace FoodieMatch.UI.Debugging
{
    public readonly struct DebugMenuValues
    {
        public DebugMenuValues(
            PlayerProfileDebugUpdate playerProfile,
            bool postLevelAdsEnabled,
            bool useLevelPlayAds)
        {
            PlayerProfile = playerProfile;
            PostLevelAdsEnabled = postLevelAdsEnabled;
            UseLevelPlayAds = useLevelPlayAds;
        }

        public PlayerProfileDebugUpdate PlayerProfile { get; }

        public bool PostLevelAdsEnabled { get; }

        public bool UseLevelPlayAds { get; }
    }
}
