using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.RetryGame
{
    public sealed class RetryGamePopupView : PopupBase
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action _retryClicked;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _retryButton.onClick.RemoveListener(OnRetryButtonClicked);
        }

        public void SetActions(RetryGamePopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _retryClicked = actions.RetryClicked;
        }

        public override void Show()
        {
            base.Show();

            _popupAnimController.Open();
        }

        public override void Hide()
        {
            if (gameObject.activeInHierarchy)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _closeClicked = null;
            _retryClicked = null;

            base.Dispose();
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
        }

        private void OnRetryButtonClicked()
        {
            _retryClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

    }
}
