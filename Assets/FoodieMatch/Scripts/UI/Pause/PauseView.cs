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

        private void Awake()
        {
            _resumeButton.onClick.AddListener(OnResumeButtonClicked);
            _restartButton.onClick.AddListener(OnRestartButtonClicked);
            _homeButton.onClick.AddListener(OnHomeButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
            _musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        private void OnDestroy()
        {
            _resumeButton.onClick.RemoveListener(OnResumeButtonClicked);
            _restartButton.onClick.RemoveListener(OnRestartButtonClicked);
            _homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
            _musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
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
            _soundToggle.SetIsOnWithoutNotify(!isSoundOn);
            _musicToggle.SetIsOnWithoutNotify(!isMusicOn);
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
            _resumeClicked = null;
            _restartClicked = null;
            _homeClicked = null;
            _closeClicked = null;
            _soundChanged = null;
            _musicChanged = null;

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
            base.Hide();
        }

    }
}
