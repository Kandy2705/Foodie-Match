using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Heart
{
    public sealed class FillHeartPopupView : PopupBase, IPlayerResourceView
    {
        [Header("Actions")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _freeAdsButton;
        [SerializeField] private Button _buyButton;

        [Header("Content")]
        [SerializeField] private TMP_Text _heartCountText;
        [SerializeField] private TMP_Text _recoveryTimerText;
        [SerializeField] private TMP_Text _coinPriceText;
        [SerializeField] private ResourceBarView _resourceBarView;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action _freeAdsClicked;
        private Action _buyClicked;
        private Action _heartRecoveredToFull;
        private DateTimeOffset _nextRecoveryAtUtc;
        private TimeSpan _recoveryDuration;
        private int _heartCount;
        private int _maxHeartCount;
        private int _displayedSecondCount = -1;
        private bool _isCountingDown;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _freeAdsButton.onClick.AddListener(OnFreeAdsButtonClicked);
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        private void Update()
        {
            if (_isCountingDown)
            {
                UpdateHeartRecovery(DateTimeOffset.UtcNow);
            }
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _freeAdsButton.onClick.RemoveListener(OnFreeAdsButtonClicked);
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }

        public void SetActions(FillHeartPopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _freeAdsClicked = actions.FreeAdsClicked;
            _buyClicked = actions.BuyClicked;
            _heartRecoveredToFull = actions.HeartRecoveredToFull;
        }

        public void SetFullHeartCoinPrice(int coinPrice)
        {
            _coinPriceText.text = coinPrice.ToString();
        }

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            _resourceBarView.SetPlayerResources(coinBalance, heartStatus);
            SetHeartStatus(heartStatus);
        }

        public override void Show()
        {
            base.Show();
            _popupAnimController.Open();
        }

        public override void Hide()
        {
            StopCountdown();

            if (gameObject.activeInHierarchy)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            StopCountdown();
            _resourceBarView.Clear();
            _closeClicked = null;
            _freeAdsClicked = null;
            _buyClicked = null;
            _heartRecoveredToFull = null;
            base.Dispose();
        }

        private void SetHeartStatus(HeartStatus heartStatus)
        {
            _heartCount = heartStatus.HeartCount;
            _maxHeartCount = heartStatus.MaxHeartCount;
            _heartCountText.text = _heartCount.ToString();

            if (heartStatus.IsFull || heartStatus.IsUnlimited)
            {
                StopCountdown();
                _heartRecoveredToFull();
                return;
            }

            _recoveryDuration = heartStatus.RecoveryDuration;
            _nextRecoveryAtUtc =
                DateTimeOffset.UtcNow + heartStatus.TimeUntilNextHeart;
            _displayedSecondCount = -1;
            _isCountingDown = true;
            UpdateHeartRecovery(DateTimeOffset.UtcNow);
        }

        private void UpdateHeartRecovery(DateTimeOffset currentUtc)
        {
            while (_heartCount < _maxHeartCount &&
                   currentUtc >= _nextRecoveryAtUtc)
            {
                _heartCount++;
                _nextRecoveryAtUtc += _recoveryDuration;
            }

            _heartCountText.text = _heartCount.ToString();

            if (_heartCount >= _maxHeartCount)
            {
                StopCountdown();
                _heartRecoveredToFull();
                return;
            }

            UpdateRecoveryTimerText(_nextRecoveryAtUtc - currentUtc);
        }

        private void UpdateRecoveryTimerText(TimeSpan remainingTime)
        {
            int totalSeconds = Math.Max(
                0,
                (int)Math.Ceiling(remainingTime.TotalSeconds));

            if (totalSeconds == _displayedSecondCount)
            {
                return;
            }

            _displayedSecondCount = totalSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _recoveryTimerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void StopCountdown()
        {
            _isCountingDown = false;
            _displayedSecondCount = -1;
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked();
        }

        private void OnFreeAdsButtonClicked()
        {
            _freeAdsClicked();
        }

        private void OnBuyButtonClicked()
        {
            _buyClicked();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
