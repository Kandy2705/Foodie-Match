using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Common;
using TMPro;
using UnityEngine;

namespace FoodieMatch.UI.Home
{
    public sealed class HeartCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _heartCountText;
        [SerializeField] private GameObject _recoveryTimerRoot;
        [SerializeField] private TMP_Text _recoveryTimerText;

        private DateTimeOffset _nextRecoveryAtUtc;
        private TimeSpan _recoveryDuration;
        private int _heartCount;
        private int _maxHeartCount;
        private int _displayedSecondCount = -1;
        private bool _isCountingDown;

        private void Update()
        {
            if (!_isCountingDown)
            {
                return;
            }

            UpdateDisplayedHeartStatus(DateTimeOffset.UtcNow);
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
            UpdateHeartCountText();

            if (heartStatus.IsFull)
            {
                StopCountdown();
                SetRecoveryTimerVisible(false);
                return;
            }

            _nextRecoveryAtUtc =
                DateTimeOffset.UtcNow + heartStatus.TimeUntilNextHeart;
            _displayedSecondCount = -1;
            _isCountingDown = true;
            SetRecoveryTimerVisible(true);
            UpdateDisplayedHeartStatus(DateTimeOffset.UtcNow);
        }

        public void Clear()
        {
            StopCountdown();
        }

        private void StopCountdown()
        {
            _isCountingDown = false;
            _displayedSecondCount = -1;
        }

        private void SetRecoveryTimerVisible(bool isVisible)
        {
            if (_recoveryTimerRoot != null)
            {
                _recoveryTimerRoot.SetActive(isVisible);
            }
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

        private void UpdateHeartCountText()
        {
            UiTmpText.SetText(_heartCountText, _heartCount.ToString());
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
            UiTmpText.SetText(
                _recoveryTimerText,
                $"{minutes:00}:{seconds:00}");
        }
    }
}
