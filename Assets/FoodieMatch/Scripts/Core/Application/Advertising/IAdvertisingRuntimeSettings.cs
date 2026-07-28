namespace FoodieMatch.Core.Application.Advertising
{
    public interface IAdvertisingRuntimeSettings
    {
        bool PostLevelAdsEnabled { get; }

        bool UseLevelPlayAds { get; }

        void Update(
            bool postLevelAdsEnabled,
            bool useLevelPlayAds);
    }
}
