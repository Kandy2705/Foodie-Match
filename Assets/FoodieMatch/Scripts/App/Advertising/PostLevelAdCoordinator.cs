using System;
using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Infrastructure.Advertising;

namespace FoodieMatch.App.Advertising
{
    public sealed class PostLevelAdCoordinator
    {
        private readonly IInterstitialAdService _interstitialAdService;
        private readonly IGameAdsConfig _adsConfig;
        private readonly IAdvertisingRuntimeSettings _runtimeSettings;
        private readonly PostLevelAdCooldown _cooldown;
        private readonly PlayerProfileService _playerProfileService;

        public PostLevelAdCoordinator(
            IInterstitialAdService interstitialAdService,
            IGameAdsConfig adsConfig,
            IAdvertisingRuntimeSettings runtimeSettings,
            PostLevelAdCooldown cooldown,
            PlayerProfileService playerProfileService)
        {
            _interstitialAdService = interstitialAdService ??
                throw new ArgumentNullException(nameof(interstitialAdService));
            _adsConfig = adsConfig ??
                throw new ArgumentNullException(nameof(adsConfig));
            _runtimeSettings = runtimeSettings ??
                throw new ArgumentNullException(nameof(runtimeSettings));
            _cooldown = cooldown ??
                throw new ArgumentNullException(nameof(cooldown));
            _playerProfileService = playerProfileService ??
                throw new ArgumentNullException(nameof(playerProfileService));
        }

        public void RunAfterPostLevelAd(
            int playedLevelNumber,
            Action continuation)
        {
            if (_playerProfileService.AdsRemoved ||
                playedLevelNumber < _adsConfig.PostLevelAdStartLevel ||
                !_runtimeSettings.PostLevelAdsEnabled ||
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
