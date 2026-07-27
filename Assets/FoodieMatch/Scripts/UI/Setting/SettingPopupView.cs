using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FoodieMatch.UI.Setting
{
    public sealed class SettingPopupView :
        PopupBase,
        IPointerClickHandler
    {
        private const int RequiredVersionTapCount = 10;

        [SerializeField] private Button _closeButton;
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private TMP_Text _versionText;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action<bool> _soundChanged;
        private Action<bool> _musicChanged;
        private Action _debugMenuRequested;
        private int _versionTapCount;

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
            _debugMenuRequested = actions.DebugMenuRequested;
        }

        public void SetToggleStates(bool isSoundOn, bool isMusicOn)
        {
            _soundToggle.SetIsOnWithoutNotify(!isSoundOn);
            _musicToggle.SetIsOnWithoutNotify(!isMusicOn);
        }

        public override void Show()
        {
            base.Show();

            _versionTapCount = 0;
            _popupAnimController.Open();
        }

        public override void Hide()
        {
            _versionTapCount = 0;

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
            _soundChanged = null;
            _musicChanged = null;
            _debugMenuRequested = null;

            base.Dispose();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerPressRaycast.gameObject !=
                _versionText.gameObject)
            {
                return;
            }

            _versionTapCount++;

            if (_versionTapCount < RequiredVersionTapCount)
            {
                return;
            }

            _versionTapCount = 0;
            _debugMenuRequested();
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked();
        }

        private void OnSoundToggleChanged(bool isOn)
        {
            bool isSoundOn = !isOn;
            _soundChanged(isSoundOn);
        }

        private void OnMusicToggleChanged(bool isOn)
        {
            bool isMusicOn = !isOn;
            _musicChanged(isMusicOn);
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
