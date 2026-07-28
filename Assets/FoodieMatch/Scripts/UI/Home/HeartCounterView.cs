using System;
using FoodieMatch.Core.Application.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Home
{
    public sealed class HeartCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _heartCountText;
        [SerializeField] private GameObject _recoveryTimerRoot;
        [SerializeField] private TMP_Text _recoveryTimerText;
        [SerializeField] private Button _lifeCounterButton;
        [SerializeField] private Image _heartIconImage;
        [SerializeField] private GameObject _addLifeButton;
        [SerializeField] private Sprite _normalHeartIconSprite;
        [SerializeField] private Sprite _unlimitedHeartIconSprite;

        private DateTimeOffset _nextRecoveryAtUtc;
        private TimeSpan _recoveryDuration;
        private int _heartCount;
        private int _maxHeartCount;
        private int _displayedSecondCount = -1;
        private bool _isCountingDown;
        private bool _isUnlimited;
        private DateTimeOffset _unlimitedHeartEndUtc;
        private TimeSpan _timeUntilNextHeart;
        private Action _clicked;

        private void Awake()
        {
            _lifeCounterButton.onClick.AddListener(OnClicked);
            _lifeCounterButton.enabled = false;
        }

        private void Update()
        {
            if (_isUnlimited)
            {
                UpdateUnlimitedHeartStatus(DateTimeOffset.UtcNow);
                return;
            }

            if (!_isCountingDown)
            {
                return;
            }

            UpdateDisplayedHeartStatus(DateTimeOffset.UtcNow);
        }

        private void OnDestroy()
        {
            _lifeCounterButton.onClick.RemoveListener(OnClicked);
        }

        public void SetHeartStatus(HeartStatus heartStatus)
        {
            if (heartStatus == null)
            {
                throw new ArgumentNullException(nameof(heartStatus));
            }

            _heartCount = heartStatus.HeartCount;
            _maxHeartCount = heartStatus.MaxHeartCount;
            _recoveryDuration = heartStatus.RecoveryDuration;
            _timeUntilNextHeart = heartStatus.TimeUntilNextHeart;

            if (heartStatus.IsUnlimited)
            {
                _isUnlimited = true;
                _unlimitedHeartEndUtc = heartStatus.UnlimitedHeartEndUtc.Value;
                StopCountdown();
                _displayedSecondCount = -1;
                SetUnlimitedPresentation(true);
                SetRecoveryTimerVisible(true);
                UpdateUnlimitedHeartStatus(DateTimeOffset.UtcNow);
                return;
            }

            _isUnlimited = false;
            SetUnlimitedPresentation(false);
            UpdateHeartCountText();

            if (heartStatus.IsFull)
            {
                StopCountdown();
                SetRecoveryTimerVisible(false);
                return;
            }

            _nextRecoveryAtUtc =
                DateTimeOffset.UtcNow + _timeUntilNextHeart;
            _displayedSecondCount = -1;
            _isCountingDown = true;
            SetRecoveryTimerVisible(true);
            UpdateDisplayedHeartStatus(DateTimeOffset.UtcNow);
        }

        public void SetClickAction(Action clicked)
        {
            _clicked = clicked;
            _lifeCounterButton.enabled = clicked != null;
        }

        public void Clear()
        {
            SetClickAction(null);
            _isUnlimited = false;
            StopCountdown();
            SetUnlimitedPresentation(false);
        }

        private void StopCountdown()
        {
            _isCountingDown = false;
            _displayedSecondCount = -1;
        }

        private void SetRecoveryTimerVisible(bool isVisible)
        {
            _recoveryTimerRoot.SetActive(isVisible);
        }

        private void UpdateDisplayedHeartStatus(DateTimeOffset currentUtc)
        {
            bool heartCountChanged = false;

            while (_heartCount < _maxHeartCount &&
                   currentUtc >= _nextRecoveryAtUtc)
            {
                _heartCount++;
                _nextRecoveryAtUtc += _recoveryDuration;
                heartCountChanged = true;
            }

            if (heartCountChanged)
            {
                UpdateHeartCountText();
            }

            if (_heartCount >= _maxHeartCount)
            {
                StopCountdown();
                SetRecoveryTimerVisible(false);
                return;
            }

            UpdateRecoveryTimerText(_nextRecoveryAtUtc - currentUtc);
        }

        private void UpdateUnlimitedHeartStatus(DateTimeOffset currentUtc)
        {
            if (currentUtc < _unlimitedHeartEndUtc)
            {
                UpdateRecoveryTimerText(_unlimitedHeartEndUtc - currentUtc);
                return;
            }

            _isUnlimited = false;
            SetUnlimitedPresentation(false);
            UpdateHeartCountText();

            if (_heartCount >= _maxHeartCount)
            {
                SetRecoveryTimerVisible(false);
                return;
            }

            _nextRecoveryAtUtc = _unlimitedHeartEndUtc + _timeUntilNextHeart;
            _displayedSecondCount = -1;
            _isCountingDown = true;
            SetRecoveryTimerVisible(true);
            UpdateDisplayedHeartStatus(currentUtc);
        }

        private void UpdateHeartCountText()
        {
            _heartCountText.text = _heartCount.ToString();
            UpdateAddLifeButtonVisibility();
        }

        private void SetUnlimitedPresentation(bool isUnlimited)
        {
            _heartCountText.text = isUnlimited ? "Unlimited" : _heartCount.ToString();
            _heartIconImage.sprite = isUnlimited
                ? _unlimitedHeartIconSprite
                : _normalHeartIconSprite;
            UpdateAddLifeButtonVisibility();
        }

        private void UpdateAddLifeButtonVisibility()
        {
            _addLifeButton.SetActive(
                !_isUnlimited &&
                _heartCount < _maxHeartCount);
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

        private void OnClicked()
        {
            _clicked();
        }
    }
}
