using System;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassRewardTrackView : MonoBehaviour
    {
        [Header("Reward")]
        [SerializeField] private Button _rewardIconButton;
        [SerializeField] private Image _rewardIconImage;
        [SerializeField] private TMP_Text _amountText;

        [Header("State")]
        [SerializeField] private Button _claimButton;
        [SerializeField] private GameObject _locker;
        [SerializeField] private GameObject _claimedTick;

        private Action _claimClicked;
        private Action<RectTransform> _treasureClicked;

        private void Awake()
        {
            _rewardIconButton.onClick.AddListener(OnRewardIconClicked);
            _claimButton.onClick.AddListener(OnClaimButtonClicked);
        }

        private void OnDestroy()
        {
            _rewardIconButton.onClick.RemoveListener(OnRewardIconClicked);
            _claimButton.onClick.RemoveListener(OnClaimButtonClicked);
        }

        public void Bind(
            GoldPassRewardDefinition reward,
            GoldPassRewardVisualCatalogSO visualCatalog,
            bool isUnlocked,
            bool isTrackAvailable,
            bool isClaimed,
            Action claimClicked,
            Action<RectTransform> treasureClicked)
        {
            _rewardIconImage.sprite = visualCatalog.GetIcon(reward);
            _rewardIconImage.preserveAspect = true;

            string amountText =
                GoldPassRewardPresentation.GetAmountText(reward);
            _amountText.gameObject.SetActive(
                !string.IsNullOrEmpty(amountText));
            _amountText.text = amountText;

            _locker.SetActive(!isUnlocked);
            _claimButton.gameObject.SetActive(
                isUnlocked && isTrackAvailable && !isClaimed);
            _claimedTick.SetActive(isClaimed);
            _rewardIconButton.enabled = reward.IsTreasure;

            _claimClicked = claimClicked;
            _treasureClicked = reward.IsTreasure
                ? treasureClicked
                : null;
        }

        public void Clear()
        {
            _claimClicked = null;
            _treasureClicked = null;
        }

        private void OnRewardIconClicked()
        {
            _treasureClicked(_rewardIconImage.rectTransform);
        }

        private void OnClaimButtonClicked()
        {
            _claimClicked();
        }
    }
}
