using System;
using FoodieMatch.Core.Application.Advertising;

namespace FoodieMatch.UI.Advertising
{
    public sealed class FakeRewardedAdService : IRewardedAdService
    {
        private readonly UIManager _uiManager;

        private RewardedAdCallbacks _callbacks;
        private bool _isAdShowing;

        public FakeRewardedAdService(UIManager uiManager)
        {
            _uiManager = uiManager ??
                throw new ArgumentNullException(nameof(uiManager));
        }

        public bool TryShow(
            RewardedAdPlacement placement,
            RewardedAdCallbacks callbacks)
        {
            ValidatePlacement(placement);

            if (_isAdShowing)
            {
                return false;
            }

            _callbacks = callbacks;
            _isAdShowing = true;

            if (!_uiManager.ShowFakeRewardedAdPopup(
                    OnAdCompleted,
                    OnAdCancelled))
            {
                ClearCurrentAd();
                return false;
            }

            return true;
        }

        private void OnAdCompleted()
        {
            RewardedAdCallbacks callbacks = FinishCurrentAd();
            callbacks.Rewarded?.Invoke();
            callbacks.Closed?.Invoke();
        }

        private void OnAdCancelled()
        {
            RewardedAdCallbacks callbacks = FinishCurrentAd();
            callbacks.Closed?.Invoke();
        }

        private RewardedAdCallbacks FinishCurrentAd()
        {
            RewardedAdCallbacks callbacks = _callbacks;

            ClearCurrentAd();
            _uiManager.HideFakeRewardedAdPopup();

            return callbacks;
        }

        private void ClearCurrentAd()
        {
            _callbacks = default;
            _isAdShowing = false;
        }

        private static void ValidatePlacement(
            RewardedAdPlacement placement)
        {
            if (!Enum.IsDefined(typeof(RewardedAdPlacement), placement))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placement),
                    placement,
                    "Rewarded ad placement is not defined.");
            }
        }
    }
}
