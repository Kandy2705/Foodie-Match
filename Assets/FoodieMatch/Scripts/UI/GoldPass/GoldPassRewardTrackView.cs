using System;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.GoldPass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassRewardTrackView : MonoBehaviour
    {
        private static readonly int ShakeState = Animator.StringToHash("shake");

        [Header("Reward")]
        [SerializeField] private Button _rewardIconButton;
        [SerializeField] private Image _rewardIconImage;
        [SerializeField] private TMP_Text _amountText;

        [Header("State")]
        [SerializeField] private Button _clickPanel;
        [SerializeField] private Button _claimButton;
        [SerializeField] private GameObject _locker;
        [SerializeField] private Animator _lockerAnimator;
        [SerializeField] private GameObject _claimedDarkOverlay;
        [SerializeField] private GameObject _claimedTick;

        private Action _claimClicked;
        private Action<RectTransform> _treasureClicked;
        private Action _lockedRewardClicked;
        private Action _purchaseClicked;
        private GoldPassTrack _track;
        private bool _isUnlocked;
        private bool _isTrackAvailable;
        private bool _isTreasure;

        private void Awake()
        {
            _rewardIconButton.onClick.AddListener(OnRewardIconClicked);
            _clickPanel.onClick.AddListener(OnClickPanelClicked);
            _claimButton.onClick.AddListener(OnClaimButtonClicked);
        }

        private void OnDestroy()
        {
            _rewardIconButton.onClick.RemoveListener(OnRewardIconClicked);
            _clickPanel.onClick.RemoveListener(OnClickPanelClicked);
            _claimButton.onClick.RemoveListener(OnClaimButtonClicked);
        }

        public void Bind(
            GoldPassRewardDefinition reward,
            GoldPassRewardVisualCatalogSO visualCatalog,
            bool isUnlocked,
            bool isTrackAvailable,
            bool isClaimed,
            GoldPassTrack track,
            Action claimClicked,
            Action<RectTransform> treasureClicked,
            Action lockedRewardClicked,
            Action purchaseClicked)
        {
            _rewardIconImage.sprite = visualCatalog.GetIcon(reward);
            _rewardIconImage.preserveAspect = true;

            string amountText =
                GoldPassRewardPresentation.GetAmountText(reward);
            _amountText.gameObject.SetActive(
                !string.IsNullOrEmpty(amountText));
            _amountText.text = amountText;

            bool showLocker =
                !isUnlocked ||
                track == GoldPassTrack.Season && !isTrackAvailable;
            _locker.SetActive(showLocker);
            _lockerAnimator.enabled = false;
            _claimButton.gameObject.SetActive(
                isUnlocked && isTrackAvailable && !isClaimed);
            _claimedDarkOverlay.SetActive(isClaimed);
            _claimedTick.SetActive(isClaimed);

            bool hasTrackClickAction =
                !isUnlocked ||
                track == GoldPassTrack.Season && !isTrackAvailable;
            _clickPanel.interactable = !isClaimed && hasTrackClickAction;
            _rewardIconButton.interactable =
                !isClaimed && (reward.IsTreasure || hasTrackClickAction);

            _claimClicked = claimClicked;
            _treasureClicked = reward.IsTreasure
                ? treasureClicked
                : null;
            _lockedRewardClicked = lockedRewardClicked;
            _purchaseClicked = purchaseClicked;
            _track = track;
            _isUnlocked = isUnlocked;
            _isTrackAvailable = isTrackAvailable;
            _isTreasure = reward.IsTreasure;
        }

        public void Clear()
        {
            _claimClicked = null;
            _treasureClicked = null;
            _lockedRewardClicked = null;
            _purchaseClicked = null;
        }

        private void OnRewardIconClicked()
        {
            if (_isTreasure)
            {
                _treasureClicked(_rewardIconImage.rectTransform);
                return;
            }

            HandleTrackClick();
        }

        private void OnClickPanelClicked()
        {
            HandleTrackClick();
        }

        private void OnClaimButtonClicked()
        {
            _claimClicked();
        }

        private void HandleTrackClick()
        {
            if (!_isUnlocked)
            {
                _lockerAnimator.enabled = true;
                _lockerAnimator.Play(ShakeState, 0, 0f);
                _lockerAnimator.Update(0f);
                _lockedRewardClicked();
                return;
            }

            if (_track == GoldPassTrack.Season && !_isTrackAvailable)
            {
                _purchaseClicked();
            }
        }
    }
}
