using System;
using FoodieMatch.Runtime.UI.Popup;
using UnityEngine;
using UnityEngine.UI;
namespace FoodieMatch.Runtime.UI.Result
{
    public sealed class LosePopupView : PopupBase
    {
        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        private Action _retryClicked;
        private Action _homeClicked;

        private void Awake()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(OnRetryButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.AddListener(OnHomeButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            }
        }

        public override void Setup(IPopupData data)
        {
            LosePopupData losePopupData = data as LosePopupData;

            if (losePopupData == null)
            {
                return;
            }

            Debug.Log($"Lose Popup Setup: Level {losePopupData.LevelId}, Reason: {losePopupData.Reason}");
        }

        public override void SetActions(
            Action primaryAction,
            Action secondaryAction)
        {
            _retryClicked = primaryAction;
            _homeClicked = secondaryAction;
        }

        public override void Dispose()
        {
            _retryClicked = null;
            _homeClicked = null;

            base.Dispose();
        }

        private void OnRetryButtonClicked()
        {
            _retryClicked?.Invoke();
        }

        private void OnHomeButtonClicked()
        {
            _homeClicked?.Invoke();
        }
    }
}
