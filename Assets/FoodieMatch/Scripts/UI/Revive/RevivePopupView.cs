using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Revive
{
    public sealed class RevivePopupView : PopupBase
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _freeAdsButton;
        [SerializeField] private Button _playOnButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action _freeAdsClicked;
        private Action _playOnClicked;
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

            if (_freeAdsButton != null)
            {
                _freeAdsButton.onClick.AddListener(OnFreeAdsButtonClicked);
            }

            if (_playOnButton != null)
            {
                _playOnButton.onClick.AddListener(OnPlayOnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (_freeAdsButton != null)
            {
                _freeAdsButton.onClick.RemoveListener(OnFreeAdsButtonClicked);
            }

            if (_playOnButton != null)
            {
                _playOnButton.onClick.RemoveListener(OnPlayOnButtonClicked);
            }
        }

        public void SetActions(RevivePopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _freeAdsClicked = actions.FreeAdsClicked;
            _playOnClicked = actions.PlayOnClicked;
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
            _freeAdsClicked = null;
            _playOnClicked = null;
            _isClosing = false;

            base.Dispose();
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
        }

        private void OnFreeAdsButtonClicked()
        {
            _freeAdsClicked?.Invoke();
        }

        private void OnPlayOnButtonClicked()
        {
            _playOnClicked?.Invoke();
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

            if (_freeAdsButton == null)
            {
                _freeAdsButton = FindChildButton("FreeAdsButton");
            }

            if (_playOnButton == null)
            {
                _playOnButton = FindChildButton("PlayOnButton");
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
