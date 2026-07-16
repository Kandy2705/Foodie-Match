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
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _resumeClicked;
        private Action _restartClicked;
        private Action _homeClicked;
        private Action _closeClicked;
        private Action<bool> _soundChanged;
        private Action<bool> _musicChanged;
        private bool _isClosing;

        private void Awake()
        {
            if (_popupAnimController == null)
            {
                _popupAnimController = GetComponent<PopupAnimController>();
            }

            EnsureButtonReferences();
            EnsureToggleReferences();

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

            if (_soundToggle != null)
            {
                _soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
            }

            if (_musicToggle != null)
            {
                _musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
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

            if (_soundToggle != null)
            {
                _soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
            }

            if (_musicToggle != null)
            {
                _musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
            }
        }

        public void SetActions(PauseViewActions actions)
        {
            _resumeClicked = actions.ResumeClicked;
            _restartClicked = actions.RestartClicked;
            _homeClicked = actions.HomeClicked;
            _closeClicked = actions.CloseClicked;
            _soundChanged = actions.SoundChanged;
            _musicChanged = actions.MusicChanged;
        }

        public void SetToggleStates(bool isSoundOn, bool isMusicOn)
        {
            if (_soundToggle != null)
            {
                _soundToggle.SetIsOnWithoutNotify(!isSoundOn);
            }

            if (_musicToggle != null)
            {
                _musicToggle.SetIsOnWithoutNotify(!isMusicOn);
            }
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
            _soundChanged = null;
            _musicChanged = null;
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

        private void OnSoundToggleChanged(bool isOn)
        {
            bool isSoundOn = !isOn;
            _soundChanged?.Invoke(isSoundOn);
        }

        private void OnMusicToggleChanged(bool isOn)
        {
            bool isMusicOn = !isOn;
            _musicChanged?.Invoke(isMusicOn);
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

            if (_resumeButton == null)
            {
                _resumeButton = FindChildButton("ResumeButton");
            }

            if (_restartButton == null)
            {
                _restartButton = FindChildButton("RestartButton");
            }

            if (_homeButton == null)
            {
                _homeButton = FindChildButton("HomeButton");
            }
        }

        private void EnsureToggleReferences()
        {
            if (_soundToggle == null)
            {
                _soundToggle = FindChildToggle("SoundToggleRoot");
            }

            if (_musicToggle == null)
            {
                _musicToggle = FindChildToggle("MusicToggleRoot");
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

        private Toggle FindChildToggle(string objectName)
        {
            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);

            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];

                if (toggle != null && toggle.gameObject.name == objectName)
                {
                    return toggle;
                }
            }

            return null;
        }
    }
}
