using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaveGame
{
    public sealed class LeaveGamePopupView : PopupBase
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action _leaveClicked;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _leaveButton.onClick.RemoveListener(OnLeaveButtonClicked);
        }

        public void SetActions(LeaveGamePopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _leaveClicked = actions.LeaveClicked;
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
            _leaveClicked = null;

            base.Dispose();
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
        }

        private void OnLeaveButtonClicked()
        {
            _leaveClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

    }
}
