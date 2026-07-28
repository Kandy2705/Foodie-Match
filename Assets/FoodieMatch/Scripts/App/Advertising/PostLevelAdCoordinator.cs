using System;
using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Infrastructure.Advertising;

namespace FoodieMatch.App.Advertising
{
    public sealed class PostLevelAdCoordinator
    {
        private readonly IInterstitialAdService _interstitialAdService;
        private readonly IGameAdsConfig _adsConfig;
        private readonly IAdvertisingRuntimeSettings _runtimeSettings;
        private readonly PostLevelAdCooldown _cooldown;

        public PostLevelAdCoordinator(
            IInterstitialAdService interstitialAdService,
            IGameAdsConfig adsConfig,
            IAdvertisingRuntimeSettings runtimeSettings,
            PostLevelAdCooldown cooldown)
        {
            _interstitialAdService = interstitialAdService;
            _adsConfig = adsConfig;
            _runtimeSettings = runtimeSettings;
            _cooldown = cooldown;
        }

        public void RunAfterPostLevelAd(Action continuation)
        {
            if (!_runtimeSettings.PostLevelAdsEnabled ||
                !_cooldown.HasElapsed(_adsConfig.PostLevelAdInterval))
            {
                continuation();
                return;
            }

            InterstitialAdCallbacks callbacks = new(
                displayed: RecordAdDisplayed,
                closed: continuation,
                displayFailed: continuation);

            if (!_interstitialAdService.TryShow(
                    InterstitialAdPlacement.PostLevel,
                    callbacks))
            {
                continuation();
            }
        }

        public void RecordAdDisplayed()
        {
            _cooldown.Restart();
        }
    }
}
