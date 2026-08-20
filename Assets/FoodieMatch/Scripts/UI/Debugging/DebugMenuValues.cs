using FoodieMatch.Core.Application.Player;

namespace FoodieMatch.UI.Debugging
{
    public readonly struct DebugMenuValues
    {
        public DebugMenuValues(
            PlayerProfileDebugUpdate playerProfile,
            int goldPassSpoonCount,
            bool isSeasonPassPurchased,
            bool adsRemoved,
            bool postLevelAdsEnabled,
            bool useLevelPlayAds)
        {
            PlayerProfile = playerProfile;
            GoldPassSpoonCount = goldPassSpoonCount;
            IsSeasonPassPurchased = isSeasonPassPurchased;
            AdsRemoved = adsRemoved;
            PostLevelAdsEnabled = postLevelAdsEnabled;
            UseLevelPlayAds = useLevelPlayAds;
        }

        public PlayerProfileDebugUpdate PlayerProfile { get; }

        public int GoldPassSpoonCount { get; }

        public bool IsSeasonPassPurchased { get; }

        public bool AdsRemoved { get; }

        public bool PostLevelAdsEnabled { get; }

        public bool UseLevelPlayAds { get; }
    }
}
