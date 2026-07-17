using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Result
{
    public sealed class WinView : PopupBase
    {
        private const string WinAnimationName = "UI_win";
        private const string IdleAnimationName = "idle";

        [Header("References")]
        [SerializeField] private Button _claimCoinRewardButton;
        [SerializeField] private Button _doubleCoinRewardButton;
        [SerializeField] private TMP_Text _rewardAmountText;
        [SerializeField] private TMP_Text _rewardMultiplierText;
        [SerializeField] private SkeletonGraphic _chefMascotSkeletonGraphic;

        private Action _claimCoinRewardClicked;
        private Action _doubleCoinRewardClicked;

        private void Awake()
        {
            EnsureTextReferences();
            EnsureMascotReference();

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

        public override void Show()
        {
            base.Show();
            PlayWinMascotAnimation();
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

        private void PlayWinMascotAnimation()
        {
            EnsureMascotReference();

            if (_chefMascotSkeletonGraphic == null)
            {
                return;
            }

            if (!_chefMascotSkeletonGraphic.IsValid)
            {
                _chefMascotSkeletonGraphic.Initialize(overwrite: false);
            }

            if (_chefMascotSkeletonGraphic.AnimationState == null)
            {
                Debug.LogWarning(
                    $"{nameof(WinView)} on {name} has no AnimationState on chef mascot.",
                    this);
                return;
            }

            // Play win once, then switch to looping idle when UI_win finishes.
            _chefMascotSkeletonGraphic.AnimationState.ClearTracks();
            _chefMascotSkeletonGraphic.AnimationState.SetAnimation(
                0,
                WinAnimationName,
                loop: false);
            _chefMascotSkeletonGraphic.AnimationState.AddAnimation(
                0,
                IdleAnimationName,
                loop: true,
                delay: 0f);
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

        private void EnsureMascotReference()
        {
            if (_chefMascotSkeletonGraphic != null)
            {
                return;
            }

            _chefMascotSkeletonGraphic =
                GetComponentInChildren<SkeletonGraphic>(includeInactive: true);
        }
    }
}
