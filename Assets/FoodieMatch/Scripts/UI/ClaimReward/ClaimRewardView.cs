using System.Collections.Generic;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.ClaimReward
{
    public sealed class ClaimRewardView : PopupBase
    {
        [Header("References")]
        [SerializeField] private Button _backgroundButton;
        [SerializeField] private PopupAnimController _popupAnimController;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private RectTransform _rewardPanel;
        [SerializeField] private ClaimRewardItemView _rewardItemPrefab;
        [SerializeField] private TMP_Text _tapToContinueText;
        [SerializeField] private Animator _tapToContinueAnimator;

        [Header("Title Reveal")]
        [SerializeField] private Vector2 _titleStartOffset = new(0f, -80f);
        [SerializeField, Range(0f, 1f)]
        private float _titleStartScaleMultiplier = 0.9f;
        [SerializeField, Min(0f)] private float _titleRevealDelay = 0.1f;
        [SerializeField, Min(0f)] private float _titleRevealDuration = 0.3f;
        [SerializeField] private Ease _titleMoveEase = Ease.OutBack;
        [SerializeField] private Ease _titleScaleEase = Ease.OutBack;
        [SerializeField] private Ease _titleFadeEase = Ease.OutCubic;

        [Header("Reward Reveal")]
        [SerializeField, Min(0f)] private float _rewardRevealDuration = 0.3f;
        [SerializeField, Min(0f)] private float _rewardRevealInterval = 0.1f;
        [SerializeField] private Ease _rewardRevealEase = Ease.OutBack;

        [Header("Continue Reveal")]
        [SerializeField, Min(0f)] private float _continueRevealDuration = 0.3f;
        [SerializeField] private Ease _continueScaleEase = Ease.OutBack;
        [SerializeField] private Ease _continueFadeEase = Ease.OutCubic;

        private static readonly int ContinueNormalState =
            Animator.StringToHash("Normal");

        private readonly List<ClaimRewardItemView> _rewardItems = new();
        private bool _isInitialized;
        private Sequence _revealSequence;
        private Vector2 _titleVisiblePosition;
        private Vector3 _titleVisibleScale;
        private float _titleVisibleAlpha;
        private Vector3 _continueVisibleScale;
        private Vector3 _continueBaseScale;
        private Vector3 _rewardPanelBaseScale;
        private float _continueVisibleAlpha;
        private float _defaultTitleFontSize;
        private ClaimRewardPopupData _data;
        private int _openCompletionCount;
        private bool _continueRequested;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _backgroundButton.onClick.AddListener(OnBackgroundClicked);
            _titleVisiblePosition = _titleText.rectTransform.anchoredPosition;
            _titleVisibleScale = _titleText.rectTransform.localScale != Vector3.zero
                ? _titleText.rectTransform.localScale
                : Vector3.one;
            _titleVisibleAlpha = _titleText.alpha > 0f ? _titleText.alpha : 1f;
            _continueBaseScale = _tapToContinueText.rectTransform.localScale != Vector3.zero
                ? _tapToContinueText.rectTransform.localScale
                : Vector3.one;
            _continueVisibleScale = _continueBaseScale;
            _rewardPanelBaseScale = _rewardPanel.localScale != Vector3.zero
                ? _rewardPanel.localScale
                : Vector3.one;
            _continueVisibleAlpha = _tapToContinueText.alpha > 0f ? _tapToContinueText.alpha : 1f;
            _defaultTitleFontSize = _titleText.fontSize > 0f ? _titleText.fontSize : 96f;
        }

        private void OnDestroy()
        {
            StopReveal();
            _backgroundButton.onClick.RemoveListener(OnBackgroundClicked);
        }

        public override void Setup(IPopupData data)
        {
            EnsureInitialized();
            _data = (ClaimRewardPopupData)data;
            _continueRequested = false;
            _titleText.text = ClaimRewardTitleText.Get(_data.Title);
            _titleText.fontSize = _data.Title == ClaimRewardTitle.Congratulations
                ? 96f
                : _defaultTitleFontSize;
            float scale = _data.PresentationScale > 0f ? _data.PresentationScale : 1f;
            _rewardPanel.localScale =
                _rewardPanelBaseScale * scale;
            _continueVisibleScale =
                _continueBaseScale * scale;
            BindRewards(_data.Rewards);
        }

        public override void Show()
        {
            base.Show();
            PrepareReveal();
            _openCompletionCount = 0;
            _popupAnimController.Open(MarkOpenPartComplete);
            PlayReveal();
        }

        public override void Hide()
        {
            StopReveal();
            _backgroundButton.interactable = false;
            _tapToContinueAnimator.enabled = false;

            if (gameObject.activeInHierarchy)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            StopReveal();
            _data = null;
            base.Dispose();
        }

        private void BindRewards(IReadOnlyList<ClaimRewardItemData> rewards)
        {
            EnsureRewardItemCount(rewards.Count);

            for (int i = 0; i < _rewardItems.Count; i++)
            {
                if (i < rewards.Count)
                {
                    _rewardItems[i].Bind(rewards[i]);
                    continue;
                }

                _rewardItems[i].Hide();
            }
        }

        private void EnsureRewardItemCount(int count)
        {
            while (_rewardItems.Count < count)
            {
                ClaimRewardItemView rewardItem = Instantiate(
                    _rewardItemPrefab,
                    _rewardPanel,
                    false);
                _rewardItems.Add(rewardItem);
            }
        }

        private void PrepareReveal()
        {
            StopReveal();
            _backgroundButton.interactable = false;

            RectTransform titleRect = _titleText.rectTransform;
            titleRect.anchoredPosition =
                _titleVisiblePosition + _titleStartOffset;
            titleRect.localScale =
                _titleVisibleScale * _titleStartScaleMultiplier;
            _titleText.alpha = 0f;

            for (int i = 0; i < _data.Rewards.Count; i++)
            {
                _rewardItems[i].PrepareReveal();
            }

            _tapToContinueAnimator.enabled = false;
            _tapToContinueText.rectTransform.localScale = Vector3.zero;
            _tapToContinueText.alpha = 0f;
        }

        private void PlayReveal()
        {
            float rewardStartTime =
                _titleRevealDelay + _titleRevealDuration;
            Sequence sequence = Sequence.Create(useUnscaledTime: true)
                .Insert(
                    _titleRevealDelay,
                    Tween.UIAnchoredPosition(
                        _titleText.rectTransform,
                        _titleVisiblePosition,
                        _titleRevealDuration,
                        _titleMoveEase))
                .Insert(
                    _titleRevealDelay,
                    Tween.Scale(
                        _titleText.rectTransform,
                        _titleVisibleScale,
                        _titleRevealDuration,
                        _titleScaleEase))
                .Insert(
                    _titleRevealDelay,
                    Tween.Alpha(
                        _titleText,
                        0f,
                        _titleVisibleAlpha,
                        _titleRevealDuration,
                        _titleFadeEase));

            for (int i = 0; i < _data.Rewards.Count; i++)
            {
                float startTime = rewardStartTime +
                                  _rewardRevealInterval * i;
                sequence = _rewardItems[i].InsertReveal(
                    sequence,
                    startTime,
                    _rewardRevealDuration,
                    _rewardRevealEase);
            }

            float rewardsEndTime = _data.Rewards.Count == 0
                ? rewardStartTime
                : rewardStartTime +
                  _rewardRevealInterval * (_data.Rewards.Count - 1) +
                  _rewardRevealDuration;

            _revealSequence = sequence
                .Insert(
                    rewardsEndTime,
                    Tween.Scale(
                        _tapToContinueText.rectTransform,
                        _continueVisibleScale,
                        _continueRevealDuration,
                        _continueScaleEase))
                .Insert(
                    rewardsEndTime,
                    Tween.Alpha(
                        _tapToContinueText,
                        0f,
                        _continueVisibleAlpha,
                        _continueRevealDuration,
                        _continueFadeEase))
                .InsertCallback(
                    rewardsEndTime + _continueRevealDuration,
                    this,
                    view => view.CompleteContentReveal());
        }

        private void CompleteContentReveal()
        {
            _tapToContinueText.rectTransform.localScale =
                _continueVisibleScale;
            _tapToContinueText.alpha = _continueVisibleAlpha;
            _tapToContinueAnimator.enabled = true;
            _tapToContinueAnimator.Play(ContinueNormalState, 0, 0f);
            _tapToContinueAnimator.Update(0f);
            MarkOpenPartComplete();
        }

        private void MarkOpenPartComplete()
        {
            _openCompletionCount++;

            if (_openCompletionCount == 2)
            {
                _backgroundButton.interactable = true;
            }
        }

        private void OnBackgroundClicked()
        {
            _continueRequested = true;
            RequestHide();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
            InvokeContinuedIfRequested();
        }

        private void InvokeContinuedIfRequested()
        {
            if (!_continueRequested)
            {
                return;
            }

            _continueRequested = false;
            _data?.Continued?.Invoke();
        }

        private void StopReveal()
        {
            if (_revealSequence.isAlive)
            {
                _revealSequence.Stop();
            }

            _revealSequence = default;
        }
    }
}
