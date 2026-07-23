using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using FoodieMatch.UI.Reward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeView : PopupBase, IPlayerResourceView
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private TMP_Text _playLevelText;
        [SerializeField] private ResourceBarView _resourceBarView;

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

        public void SetCoinBalance(long coinBalance)
        {
            _resourceBarView?.SetCoinBalance(coinBalance);
        }

        public void SetHeartStatus(HeartStatus heartStatus)
        {
            _resourceBarView?.SetHeartStatus(heartStatus);
        }

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            _resourceBarView?.SetPlayerResources(
                coinBalance,
                heartStatus);
        }

        public CoinCounterView GetCoinCounter()
        {
            return _resourceBarView?.CoinCounterView;
        }

        public override void Dispose()
        {
            _playClicked = null;
            _settingClicked = null;
            _resourceBarView?.Clear();

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
