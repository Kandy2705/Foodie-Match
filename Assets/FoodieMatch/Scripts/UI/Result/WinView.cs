using System;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Result
{
    public sealed class WinView : PopupBase
    {
        [SerializeField] private Button _claimCoinRewardButton;
        [SerializeField] private Button _doubleCoinRewardButton;

        private Action _claimCoinRewardClicked;
        private Action _doubleCoinRewardClicked;

        private void Awake()
        {
            if (_claimCoinRewardButton != null)
            {
                _claimCoinRewardButton.onClick.AddListener(OnClaimCoinRewardButtonClicked);
            }

            if (_doubleCoinRewardButton != null)
            {
                _doubleCoinRewardButton.onClick.AddListener(OnDoubleCoinRewardButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_claimCoinRewardButton != null)
            {
                _claimCoinRewardButton.onClick.RemoveListener(OnClaimCoinRewardButtonClicked);
            }

            if (_doubleCoinRewardButton != null)
            {
                _doubleCoinRewardButton.onClick.RemoveListener(OnDoubleCoinRewardButtonClicked);
            }
        }

        public void SetActions(WinViewActions actions)
        {
            _claimCoinRewardClicked = actions.ClaimCoinRewardClicked;
            _doubleCoinRewardClicked = actions.DoubleCoinRewardClicked;
        }

        public override void Dispose()
        {
            _claimCoinRewardClicked = null;
            _doubleCoinRewardClicked = null;

            base.Dispose();
        }

        private void OnClaimCoinRewardButtonClicked()
        {
            _claimCoinRewardClicked?.Invoke();
        }

        private void OnDoubleCoinRewardButtonClicked()
        {
            _doubleCoinRewardClicked?.Invoke();
        }
    }
}
