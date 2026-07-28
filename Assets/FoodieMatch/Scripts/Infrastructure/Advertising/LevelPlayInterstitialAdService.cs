using FoodieMatch.Core.Application.Advertising;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Advertising
{
    public sealed class LevelPlayInterstitialAdService : IInterstitialAdService
    {
        private readonly LevelPlayAdsInitializer _initializer;
        private readonly string _adUnitId;

        private LevelPlayInterstitialAd _ad;
        private InterstitialAdCallbacks _callbacks;
        private AdState _state;

        public LevelPlayInterstitialAdService(
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

        public bool TryShow(InterstitialAdPlacement placement, InterstitialAdCallbacks callbacks)
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
                LevelPlayInterstitialAd.IsPlacementCapped(placementName))
            {
                return false;
            }

            _callbacks = callbacks;
            _state = AdState.Showing;
            _ad.ShowAd(placementName);
            return true;
        }

        private void CreateAndLoadAd()
        {
            _ad = new LevelPlayInterstitialAd(_adUnitId);
            _ad.OnAdLoaded += OnAdLoaded;
            _ad.OnAdLoadFailed += OnAdLoadFailed;
            _ad.OnAdDisplayed += OnAdDisplayed;
            _ad.OnAdDisplayFailed += OnAdDisplayFailed;
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
            Debug.LogError(
                $"LevelPlay interstitial ad failed to load: {error}");
        }

        private void OnAdDisplayed(LevelPlayAdInfo adInfo)
        {
            _callbacks.Displayed?.Invoke();
        }

        private void OnAdDisplayFailed(
            LevelPlayAdInfo adInfo,
            LevelPlayAdError error)
        {
            InterstitialAdCallbacks callbacks = FinishCurrentAd();
            callbacks.DisplayFailed?.Invoke();
            Debug.LogError(
                $"LevelPlay interstitial ad failed to display: {error}");
            LoadAd();
        }

        private void OnAdClosed(LevelPlayAdInfo adInfo)
        {
            InterstitialAdCallbacks callbacks = FinishCurrentAd();
            callbacks.Closed?.Invoke();
            LoadAd();
        }

        private InterstitialAdCallbacks FinishCurrentAd()
        {
            InterstitialAdCallbacks callbacks = _callbacks;
            _callbacks = default;
            _state = AdState.Unavailable;
            return callbacks;
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
