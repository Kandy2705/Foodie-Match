using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeView : PopupBase
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private TMP_Text _playLevelText;

        private Action _playClicked;
        private Action _settingClicked;

        private void Awake()
        {
            EnsureButtonReferences();
            EnsureTextReferences();

            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            if (_settingButton != null)
            {
                _settingButton.onClick.AddListener(OnSettingButtonClicked);
            }
            else
            {
                Debug.LogWarning($"{nameof(HomeView)} on {name} has no setting button assigned.");
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            }

            if (_settingButton != null)
            {
                _settingButton.onClick.RemoveListener(OnSettingButtonClicked);
            }
        }

        public void SetActions(HomeViewActions actions)
        {
            _playClicked = actions.PlayClicked;
            _settingClicked = actions.SettingClicked;
        }

        public void SetPlayLevelNumber(int levelNumber)
        {
            EnsureTextReferences();
            UiTmpText.SetText(_playLevelText, $"Level {levelNumber}");
        }

        public override void Dispose()
        {
            _playClicked = null;
            _settingClicked = null;

            base.Dispose();
        }

        private void OnPlayButtonClicked()
        {
            _playClicked?.Invoke();
        }

        private void OnSettingButtonClicked()
        {
            _settingClicked?.Invoke();
        }

        private void EnsureButtonReferences()
        {
            if (_settingButton == null)
            {
                _settingButton = FindChildButton("SettingsButton");
            }

            if (_playButton == null)
            {
                _playButton = FindChildButton("PlayLevelButton");
            }

            if (_playButton == null)
            {
                _playButton = FindChildButton("PlayButton");
            }
        }

        private void EnsureTextReferences()
        {
            if (_playLevelText != null)
            {
                return;
            }

            if (_playButton != null)
            {
                _playLevelText = _playButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (_playLevelText == null)
            {
                _playLevelText = UiTmpText.FindChild(transform, "Text (TMP)");
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
