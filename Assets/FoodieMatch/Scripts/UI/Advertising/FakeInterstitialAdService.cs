using FoodieMatch.Core.Application.Advertising;

namespace FoodieMatch.UI.Advertising
{
    public sealed class FakeInterstitialAdService : IInterstitialAdService
    {
        private readonly UIManager _uiManager;

        private InterstitialAdCallbacks _callbacks;
        private bool _isAdShowing;

        public FakeInterstitialAdService(UIManager uiManager)
        {
            _uiManager = uiManager;
        }

        public bool TryShow(
            InterstitialAdPlacement placement,
            InterstitialAdCallbacks callbacks)
        {
            if (_isAdShowing)
            {
                return false;
            }

            _callbacks = callbacks;
            _isAdShowing = true;
            _uiManager.ShowFakeRewardedAdPopup(
                OnAdClosed,
                OnAdClosed);
            callbacks.Displayed?.Invoke();
            return true;
        }

        private void OnAdClosed()
        {
            InterstitialAdCallbacks callbacks = _callbacks;
            _callbacks = default;
            _isAdShowing = false;
            _uiManager.HideFakeRewardedAdPopup();
            callbacks.Closed?.Invoke();
        }
    }
}
