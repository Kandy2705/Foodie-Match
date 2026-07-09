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
        private bool _isClosing;

        private void Awake()
        {
            if (_popupAnimController == null)
            {
                _popupAnimController = GetComponent<PopupAnimController>();
            }

            EnsureButtonReferences();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (_leaveButton != null)
            {
                _leaveButton.onClick.AddListener(OnLeaveButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (_leaveButton != null)
            {
                _leaveButton.onClick.RemoveListener(OnLeaveButtonClicked);
            }
        }

        public void SetActions(LeaveGamePopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _leaveClicked = actions.LeaveClicked;
        }

        public override void Show()
        {
            _isClosing = false;
            base.Show();

            if (_popupAnimController != null)
            {
                _popupAnimController.Open();
            }
        }

        public override void Hide()
        {
            if (_isClosing)
            {
                return;
            }

            if (_popupAnimController != null && gameObject.activeInHierarchy)
            {
                _isClosing = true;
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _closeClicked = null;
            _leaveClicked = null;
            _isClosing = false;

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
            _isClosing = false;
            base.Hide();
        }

        private void EnsureButtonReferences()
        {
            if (_closeButton == null)
            {
                _closeButton = FindChildButton("CloseButton");
            }

            if (_leaveButton == null)
            {
                _leaveButton = FindChildButton("LeaveButton");
            }

            if (_leaveButton == null)
            {
                _leaveButton = FindChildButton("PrimaryButton");
            }
        }

        private Button FindChildButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];

                if (button != null && button.gameObject.name == objectName)
                {
                    return button;
                }
            }

            return null;
        }
    }
}
