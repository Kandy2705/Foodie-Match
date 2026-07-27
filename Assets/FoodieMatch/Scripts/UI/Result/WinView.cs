using System;
using FoodieMatch.UI.Popup;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
        [SerializeField] private TMP_Text _regularRewardAmountText;
        [SerializeField] private TMP_Text _doubleRewardAmountText;
        [SerializeField] private SkeletonGraphic _chefMascotSkeletonGraphic;

        private Action _claimCoinRewardClicked;
        private Action _doubleCoinRewardClicked;

        private void Awake()
        {
            _claimCoinRewardButton.onClick.AddListener(OnClaimCoinRewardButtonClicked);
            _doubleCoinRewardButton.onClick.AddListener(OnDoubleCoinRewardButtonClicked);
        }

        private void OnDestroy()
        {
            _claimCoinRewardButton.onClick.RemoveListener(OnClaimCoinRewardButtonClicked);
            _doubleCoinRewardButton.onClick.RemoveListener(OnDoubleCoinRewardButtonClicked);
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

        public void SetRewardAmounts(long regularRewardAmount, long doubleRewardAmount)
        {
            _regularRewardAmountText.text = Math.Max(0, regularRewardAmount).ToString();
            _doubleRewardAmountText.text = Math.Max(0, doubleRewardAmount).ToString();
        }

        public override void Dispose()
        {
            _claimCoinRewardClicked = null;
            _doubleCoinRewardClicked = null;

            base.Dispose();
        }

        private void PlayWinMascotAnimation()
        {
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

    }
}
