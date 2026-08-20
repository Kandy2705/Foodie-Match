using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.GoldPass;
using FoodieMatch.UI.ClaimReward;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassView : PopupBase
    {
        [Header("Actions")]
        [SerializeField] private Button _informationButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _activateButton;
        [SerializeField] private Button _seasonPassButton;

        [Header("Status")]
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private SlicedProgressBarView _progressBar;
        [SerializeField] private GameObject _nextLevelPanel;
        [SerializeField] private TMP_Text _nextLevelText;

        [Header("Milestones")]
        [SerializeField] private ScrollRect _rewardsScrollView;
        [SerializeField] private RectTransform _milestoneContent;
        [SerializeField] private GoldPassMilestoneView _milestonePrefab;
        [SerializeField] private GoldPassRewardVisualCatalogSO _visualCatalog;
        [SerializeField] private GoldPassRewardPreviewView _rewardPreview;

        [Header("Visibility")]
        [SerializeField] private PopupAnimController _animController;

        private readonly List<GoldPassMilestoneView> _milestoneViews = new();
        private Action _closeClicked;
        private Action _informationClicked;
        private Action _purchaseClicked;
        private Action _lockedRewardClicked;
        private Action<int, GoldPassTrack, ClaimRewardPopupData> _claimClicked;
        private Action _seasonExpired;
        private DateTimeOffset _seasonEndUtc;
        private int _scrollTargetIndex;
        private int _displayedMinuteCount = -1;
        private bool _isCountingDown;

        private void Awake()
        {
            _informationButton.onClick.AddListener(OnInformationButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _activateButton.onClick.AddListener(OnPurchaseButtonClicked);
            _seasonPassButton.onClick.AddListener(OnPurchaseButtonClicked);
            _rewardPreview.Hide();
        }

        private void Update()
        {
            if (_isCountingDown)
            {
                UpdateCountdown(DateTimeOffset.UtcNow);
            }
        }

        private void OnDestroy()
        {
            _informationButton.onClick.RemoveListener(
                OnInformationButtonClicked);
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _activateButton.onClick.RemoveListener(OnPurchaseButtonClicked);
            _seasonPassButton.onClick.RemoveListener(OnPurchaseButtonClicked);
        }

        public void SetActions(GoldPassViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _informationClicked = actions.InformationClicked;
            _purchaseClicked = actions.PurchaseClicked;
            _lockedRewardClicked = actions.LockedRewardClicked;
            _claimClicked = actions.ClaimClicked;
            _seasonExpired = actions.SeasonExpired;
        }

        public void Bind(GoldPassStatus status)
        {
            _seasonEndUtc = status.Season.EndUtc;
            _displayedMinuteCount = -1;
            _isCountingDown = true;
            UpdateCountdown(DateTimeOffset.UtcNow);
            BindProgress(status);
            BindMilestones(status);
            BindSeasonPass(status.IsSeasonPassPurchased);
        }

        public override void Show()
        {
            base.Show();
            _animController.Open();
        }

        public void ScrollToCurrentMilestone()
        {
            if (_milestoneViews.Count == 0)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_milestoneContent);

            float normalizedPosition = _milestoneViews.Count == 1
                ? 1f
                : 1f -
                  (float)_scrollTargetIndex /
                  (_milestoneViews.Count - 1);
            _rewardsScrollView.verticalNormalizedPosition =
                normalizedPosition;
        }

        public override void Hide()
        {
            _isCountingDown = false;
            _rewardPreview.Hide();

            if (gameObject.activeInHierarchy)
            {
                _animController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _isCountingDown = false;
            _rewardPreview.Hide();

            for (int i = 0; i < _milestoneViews.Count; i++)
            {
                _milestoneViews[i].Clear();
            }

            _closeClicked = null;
            _informationClicked = null;
            _purchaseClicked = null;
            _lockedRewardClicked = null;
            _claimClicked = null;
            _seasonExpired = null;
            base.Dispose();
        }

        private void BindProgress(GoldPassStatus status)
        {
            if (status.IsComplete)
            {
                _progressBar.SetProgress(1f);
                _progressText.text = "FULL";
                _nextLevelPanel.SetActive(false);
                return;
            }

            _progressBar.SetProgress(
                (float)status.CurrentSegmentSpoons /
                status.RequiredSegmentSpoons);
            _progressText.text =
                $"{status.CurrentSegmentSpoons}/{status.RequiredSegmentSpoons}";
            _nextLevelText.text = status.NextMilestoneLevel.Value.ToString();
            _nextLevelPanel.SetActive(true);
        }

        private void BindMilestones(GoldPassStatus status)
        {
            EnsureMilestoneViewCount(status.Milestones.Count);

            int currentMilestoneIndex = 0;
            _scrollTargetIndex = status.Milestones.Count - 1;

            for (int i = 0; i < status.Milestones.Count; i++)
            {
                GoldPassMilestoneStatus milestone = status.Milestones[i];

                if (milestone.IsUnlocked)
                {
                    currentMilestoneIndex = i;
                }
                else if (_scrollTargetIndex == status.Milestones.Count - 1)
                {
                    _scrollTargetIndex = i;
                }
            }

            for (int i = 0; i < _milestoneViews.Count; i++)
            {
                if (i >= status.Milestones.Count)
                {
                    _milestoneViews[i].gameObject.SetActive(false);
                    continue;
                }

                _milestoneViews[i].Bind(
                    status.Milestones[i],
                    status.IsSeasonPassPurchased,
                    i == currentMilestoneIndex,
                    _visualCatalog,
                    OnClaimClicked,
                    OnTreasureClicked,
                    _lockedRewardClicked,
                    _purchaseClicked);
            }
        }

        private void EnsureMilestoneViewCount(int count)
        {
            while (_milestoneViews.Count < count)
            {
                GoldPassMilestoneView milestoneView = Instantiate(
                    _milestonePrefab,
                    _milestoneContent,
                    false);
                _milestoneViews.Add(milestoneView);
            }
        }

        private void BindSeasonPass(bool isPurchased)
        {
            _activateButton.gameObject.SetActive(!isPurchased);
            _seasonPassButton.interactable = !isPurchased;
        }

        private void UpdateCountdown(DateTimeOffset currentUtc)
        {
            TimeSpan remaining = _seasonEndUtc - currentUtc;

            if (remaining <= TimeSpan.Zero)
            {
                _timeText.text = "0m";
                _isCountingDown = false;
                _seasonExpired();
                return;
            }

            int totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);

            if (totalMinutes == _displayedMinuteCount)
            {
                return;
            }

            _displayedMinuteCount = totalMinutes;
            int days = totalMinutes / (24 * 60);
            int hours = totalMinutes / 60 % 24;
            int minutes = totalMinutes % 60;

            if (days > 0)
            {
                _timeText.text = $"{days}d {hours}h";
                return;
            }

            if (hours > 0)
            {
                _timeText.text = $"{hours}h {minutes}m";
                return;
            }

            _timeText.text = $"{minutes}m";
        }

        private void OnTreasureClicked(
            int milestoneLevel,
            GoldPassTrack track,
            GoldPassRewardDefinition treasure,
            RectTransform source)
        {
            _rewardPreview.Toggle(
                milestoneLevel,
                track,
                treasure,
                _visualCatalog,
                source);
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked();
        }

        private void OnInformationButtonClicked()
        {
            _informationClicked();
        }

        private void OnPurchaseButtonClicked()
        {
            _purchaseClicked();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

        private void OnClaimClicked(
            int milestoneLevel,
            GoldPassTrack track,
            GoldPassRewardDefinition reward)
        {
            _rewardPreview.Hide();
            _claimClicked(
                milestoneLevel,
                track,
                GoldPassRewardPresentation.CreateClaimPopupData(
                    reward,
                    _visualCatalog));
        }

    }
}
