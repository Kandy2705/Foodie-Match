using System;
using System.Threading.Tasks;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.GoldPass
{
    [DisallowMultipleComponent]
    public sealed class GoldPassPurchaseView : PopupBase
    {
        [Header("Actions")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _buyButton;

        [Header("Status")]
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _timeText;

        [Header("Visibility")]
        [SerializeField] private PopupAnimController _animController;

        private Func<Task> _buyClicked;
        private Action _seasonExpired;
        private DateTimeOffset _seasonEndUtc;
        private int _displayedMinuteCount = -1;
        private bool _isCountingDown;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
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
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }

        public void SetActions(GoldPassPurchaseViewActions actions)
        {
            _buyClicked = actions.BuyClicked;
            _seasonExpired = actions.SeasonExpired;
        }

        public void Bind(
            string displayPrice,
            DateTimeOffset seasonEndUtc)
        {
            _priceText.text = displayPrice;
            _seasonEndUtc = seasonEndUtc;
            _displayedMinuteCount = -1;
            _isCountingDown = true;
            UpdateCountdown(DateTimeOffset.UtcNow);
        }

        public override void Show()
        {
            base.Show();
            _animController.Open();
        }

        public override void Hide()
        {
            _isCountingDown = false;

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
            _buyClicked = null;
            _seasonExpired = null;
            base.Dispose();
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

        private void OnCloseButtonClicked()
        {
            RequestHide();
        }

        private async void OnBuyButtonClicked()
        {
            _buyButton.interactable = false;

            try
            {
                await _buyClicked();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                if (this != null)
                {
                    _buyButton.interactable = true;
                }
            }
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
