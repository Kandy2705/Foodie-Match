using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Pause
{
    public sealed class PauseView : PopupBase
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _resumeClicked;
        private Action _restartClicked;
        private Action _homeClicked;
        private Action _closeClicked;
        private bool _isClosing;

        private void Awake()
        {
            if (_popupAnimController == null)
            {
                _popupAnimController = GetComponent<PopupAnimController>();
            }

            EnsureButtonReferences();

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(OnResumeButtonClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.AddListener(OnHomeButtonClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
            else
            {
                Debug.LogWarning($"{nameof(PauseView)} on {name} has no close button assigned.");
            }
        }

        private void OnDestroy()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(OnResumeButtonClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        public void SetActions(PauseViewActions actions)
        {
            _resumeClicked = actions.ResumeClicked;
            _restartClicked = actions.RestartClicked;
            _homeClicked = actions.HomeClicked;
            _closeClicked = actions.CloseClicked;
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
            _resumeClicked = null;
            _restartClicked = null;
            _homeClicked = null;
            _closeClicked = null;
            _isClosing = false;

            base.Dispose();
        }

        private void OnResumeButtonClicked()
        {
            _resumeClicked?.Invoke();
        }

        private void OnRestartButtonClicked()
        {
            _restartClicked?.Invoke();
        }

        private void OnHomeButtonClicked()
        {
            _homeClicked?.Invoke();
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
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
