using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Result
{
    public sealed class WinView : PopupBase
    {
        [SerializeField] private Button _claimCoinRewardButton;
        [SerializeField] private Button _doubleCoinRewardButton;
        [SerializeField] private TMP_Text _rewardAmountText;
        [SerializeField] private TMP_Text _rewardMultiplierText;

        private Action _claimCoinRewardClicked;
        private Action _doubleCoinRewardClicked;

        private void Awake()
        {
            EnsureTextReferences();

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

        public void SetRewardAmount(string amountText)
        {
            EnsureTextReferences();
            UiTmpText.SetText(_rewardAmountText, amountText);
        }

        public void SetRewardMultiplier(string multiplierText)
        {
            EnsureTextReferences();
            UiTmpText.SetText(_rewardMultiplierText, multiplierText);
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

        private void EnsureTextReferences()
        {
            if (_rewardAmountText == null)
            {
                _rewardAmountText = UiTmpText.FindChild(transform, "RewardAmountText");
            }

            if (_rewardMultiplierText == null)
            {
                _rewardMultiplierText = UiTmpText.FindChild(transform, "RewardMultiplierText");
            }
        }
    }
}
