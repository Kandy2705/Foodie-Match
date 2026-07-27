using System;
using FoodieMatch.UI.Popup;
using PrimeTween;
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
        private const float ButtonOvershootScale = 1.3f;

        [Header("References")]
        [SerializeField] private Button _claimCoinRewardButton;
        [SerializeField] private Button _doubleCoinRewardButton;
        [SerializeField] private TMP_Text _regularRewardAmountText;
        [SerializeField] private TMP_Text _doubleRewardAmountText;
        [SerializeField] private SkeletonGraphic _chefMascotSkeletonGraphic;

        [Header("Button Reveal")]
        [SerializeField, Min(0f)] private float _buttonRevealDelay = 0.2f;
        [SerializeField, Min(0f)] private float _buttonScaleUpDuration = 0.18f;
        [SerializeField, Min(0f)] private float _buttonSettleDuration = 0.12f;

        private Action _claimCoinRewardClicked;
        private Action _doubleCoinRewardClicked;
        private Animator _claimCoinRewardButtonAnimator;
        private Animator _doubleCoinRewardButtonAnimator;
        private Sequence _buttonRevealSequence;

        private void Awake()
        {
            _claimCoinRewardButtonAnimator =
                _claimCoinRewardButton.GetComponent<Animator>();
            _doubleCoinRewardButtonAnimator =
                _doubleCoinRewardButton.GetComponent<Animator>();

            _claimCoinRewardButton.onClick.AddListener(OnClaimCoinRewardButtonClicked);
            _doubleCoinRewardButton.onClick.AddListener(OnDoubleCoinRewardButtonClicked);
            PrepareRewardButtonsForReveal();
        }

        private void OnDestroy()
        {
            StopButtonRevealAnimation();
            _claimCoinRewardButton.onClick.RemoveListener(OnClaimCoinRewardButtonClicked);
            _doubleCoinRewardButton.onClick.RemoveListener(OnDoubleCoinRewardButtonClicked);
        }

        public override void Show()
        {
            base.Show();
            PlayWinMascotAnimation();
            PlayButtonRevealAnimation();
        }

        public override void Hide()
        {
            StopButtonRevealAnimation();
            RestoreRewardButtons();
            base.Hide();
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

        private void PlayButtonRevealAnimation()
        {
            StopButtonRevealAnimation();
            PrepareRewardButtonsForReveal();

            float revealDelay = Mathf.Max(0f, _buttonRevealDelay);
            float scaleUpDuration = Mathf.Max(0f, _buttonScaleUpDuration);
            float settleDuration = Mathf.Max(0f, _buttonSettleDuration);
            Vector3 overshootScale = Vector3.one * ButtonOvershootScale;

            _buttonRevealSequence = Sequence.Create(useUnscaledTime: true)
                .ChainDelay(revealDelay)
                .Chain(Tween.Scale(
                    _doubleCoinRewardButton.transform,
                    overshootScale,
                    scaleUpDuration,
                    Ease.OutQuad))
                .Chain(Tween.Scale(
                    _doubleCoinRewardButton.transform,
                    Vector3.one,
                    settleDuration,
                    Ease.OutBack))
                .Chain(Tween.Scale(
                    _claimCoinRewardButton.transform,
                    overshootScale,
                    scaleUpDuration,
                    Ease.OutQuad))
                .Chain(Tween.Scale(
                    _claimCoinRewardButton.transform,
                    Vector3.one,
                    settleDuration,
                    Ease.OutBack))
                .ChainCallback(this, view => view.EnableRewardButtons());
        }

        private void PrepareRewardButtonsForReveal()
        {
            SetRewardButtonAnimatorsEnabled(false);
            _doubleCoinRewardButton.interactable = false;
            _claimCoinRewardButton.interactable = false;
            _doubleCoinRewardButton.transform.localScale = Vector3.zero;
            _claimCoinRewardButton.transform.localScale = Vector3.zero;
        }

        private void EnableRewardButtons()
        {
            _doubleCoinRewardButton.interactable = true;
            _claimCoinRewardButton.interactable = true;
            SetRewardButtonAnimatorsEnabled(true);
        }

        private void RestoreRewardButtons()
        {
            SetRewardButtonAnimatorsEnabled(false);
            _doubleCoinRewardButton.transform.localScale = Vector3.one;
            _claimCoinRewardButton.transform.localScale = Vector3.one;
            EnableRewardButtons();
        }

        private void SetRewardButtonAnimatorsEnabled(bool isEnabled)
        {
            _doubleCoinRewardButtonAnimator.enabled = isEnabled;
            _claimCoinRewardButtonAnimator.enabled = isEnabled;
        }

        private void StopButtonRevealAnimation()
        {
            if (_buttonRevealSequence.isAlive)
            {
                _buttonRevealSequence.Stop();
            }

            _buttonRevealSequence = default;
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
