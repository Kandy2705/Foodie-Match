using FoodieMatch.Core.Application.Advertising;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Advertising
{
    public sealed class LevelPlayRewardedAdService : IRewardedAdService
    {
        private readonly LevelPlayAdsInitializer _initializer;
        private readonly string _adUnitId;

        private LevelPlayRewardedAd _ad;
        private RewardedAdCallbacks _callbacks;
        private AdState _state;
        private bool _rewardReceived;
        private bool _adClosed;

        public LevelPlayRewardedAdService(
            LevelPlayAdsInitializer initializer,
            string adUnitId)
        {
            _initializer = initializer;
            _adUnitId = adUnitId;

            if (_initializer.IsInitialized)
            {
                CreateAndLoadAd();
            }
            else
            {
                _initializer.Initialized += CreateAndLoadAd;
            }
        }

        public bool TryShow(RewardedAdPlacement placement, RewardedAdCallbacks callbacks)
        {
            if (_ad == null)
            {
                _initializer.Initialize();
                return false;
            }

            if (_state == AdState.Unavailable)
            {
                LoadAd();
                return false;
            }

            string placementName = LevelPlayAdPlacementNames.GetName(placement);

            if (_state != AdState.Ready ||
                !_ad.IsAdReady() ||
                LevelPlayRewardedAd.IsPlacementCapped(placementName))
            {
                return false;
            }

            _callbacks = callbacks;
            _rewardReceived = false;
            _adClosed = false;
            _state = AdState.Showing;
            _ad.ShowAd(placementName);
            return true;
        }

        private void CreateAndLoadAd()
        {
            _ad = new LevelPlayRewardedAd(_adUnitId);
            _ad.OnAdLoaded += OnAdLoaded;
            _ad.OnAdLoadFailed += OnAdLoadFailed;
            _ad.OnAdDisplayed += OnAdDisplayed;
            _ad.OnAdDisplayFailed += OnAdDisplayFailed;
            _ad.OnAdRewarded += OnAdRewarded;
            _ad.OnAdClosed += OnAdClosed;
            LoadAd();
        }

        private void LoadAd()
        {
            _state = AdState.Loading;
            _ad.LoadAd();
        }

        private void OnAdLoaded(LevelPlayAdInfo adInfo)
        {
            _state = AdState.Ready;
        }

        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            _state = AdState.Unavailable;
            Debug.LogError($"LevelPlay rewarded ad failed to load: {error}");
        }

        private void OnAdDisplayed(LevelPlayAdInfo adInfo)
        {
            _callbacks.Displayed?.Invoke();
        }

        private void OnAdDisplayFailed(
            LevelPlayAdInfo adInfo,
            LevelPlayAdError error)
        {
            RewardedAdCallbacks callbacks = _callbacks;
            ClearCallbacks();
            _state = AdState.Unavailable;
            callbacks.DisplayFailed?.Invoke();
            Debug.LogError($"LevelPlay rewarded ad failed to display: {error}");
            LoadAd();
        }

        private void OnAdRewarded(
            LevelPlayAdInfo adInfo,
            LevelPlayReward reward)
        {
            _rewardReceived = true;
            _callbacks.Rewarded?.Invoke();
            ClearCallbacksIfFinished();
        }

        private void OnAdClosed(LevelPlayAdInfo adInfo)
        {
            _adClosed = true;
            _state = AdState.Unavailable;
            _callbacks.Closed?.Invoke();
            ClearCallbacksIfFinished();
            LoadAd();
        }

        private void ClearCallbacksIfFinished()
        {
            if (_rewardReceived && _adClosed)
            {
                ClearCallbacks();
            }
        }

        private void ClearCallbacks()
        {
            _callbacks = default;
            _rewardReceived = false;
            _adClosed = false;
        }

        private enum AdState
        {
            Unavailable,
            Loading,
            Ready,
            Showing
        }
    }
}
