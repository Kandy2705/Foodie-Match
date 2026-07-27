using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Setting
{
    public sealed class SettingPopupView : PopupBase
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action<bool> _soundChanged;
        private Action<bool> _musicChanged;
        private bool _isClosing;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
            _musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
            _musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
        }

        public void SetActions(SettingPopupViewActions actions)
        {
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
            _isClosing = false;
            base.Show();

            _popupAnimController.Open();
        }

        public override void Hide()
        {
            if (_isClosing)
            {
                return;
            }

            if (gameObject.activeInHierarchy)
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
            _soundChanged = null;
            _musicChanged = null;
            _isClosing = false;

            base.Dispose();
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
    }
}
