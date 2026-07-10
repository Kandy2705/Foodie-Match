using System;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeView : PopupBase
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingButton;

        private Action _playClicked;
        private Action _settingClicked;

        private void Awake()
        {
            EnsureButtonReferences();

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
