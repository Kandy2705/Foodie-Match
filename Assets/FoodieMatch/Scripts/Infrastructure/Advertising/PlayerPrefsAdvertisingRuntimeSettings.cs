using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Infrastructure.Persistence.Save;

namespace FoodieMatch.Infrastructure.Advertising
{
    public sealed class PlayerPrefsAdvertisingRuntimeSettings : IAdvertisingRuntimeSettings
    {
        private const string PostLevelAdsEnabledKey =
            "Advertising.PostLevelAdsEnabled";
        private const string UseLevelPlayAdsKey =
            "Advertising.UseLevelPlayAds";
        private const int Enabled = 1;
        private const int Disabled = 0;

        private readonly ISaveService _saveService;

        public PlayerPrefsAdvertisingRuntimeSettings(
            ISaveService saveService)
        {
            _saveService = saveService;
        }

        public bool PostLevelAdsEnabled =>
            _saveService.GetInt(
                PostLevelAdsEnabledKey,
                defaultValue: Enabled) == Enabled;

        public bool UseLevelPlayAds =>
            _saveService.GetInt(
                UseLevelPlayAdsKey,
                defaultValue: Enabled) == Enabled;

        public void Update(
            bool postLevelAdsEnabled,
            bool useLevelPlayAds)
        {
            _saveService.SetInt(
                PostLevelAdsEnabledKey,
                postLevelAdsEnabled ? Enabled : Disabled);
            _saveService.SetInt(
                UseLevelPlayAdsKey,
                useLevelPlayAds ? Enabled : Disabled);
            _saveService.Save();
        }
    }
}
