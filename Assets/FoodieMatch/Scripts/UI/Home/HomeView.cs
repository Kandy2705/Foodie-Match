using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.MainMenu;
using FoodieMatch.UI.Reward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeView : MonoBehaviour, IPlayerResourceView, IMainMenuViewLifecycle
    {
        private const string StarterPackButtonName = "StarterPackButton";

        [Header("Actions")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingButton;
        [SerializeField] private Button _starterPackButton;
        [SerializeField] private Button _goldPassButton;
        [Header("Content")]
        [SerializeField] private TMP_Text _playLevelText;
        [SerializeField] private ResourceBarView _resourceBarView;
        [SerializeField] private Sprite _hardPlayButtonSprite;
        [SerializeField] private Sprite _superHardPlayButtonSprite;
        [SerializeField] private TMP_FontAsset _normalPlayLevelFont;
        [SerializeField] private TMP_FontAsset _hardPlayLevelFont;
        [SerializeField] private TMP_FontAsset _superHardPlayLevelFont;

        private Action _playClicked;
        private Action _settingClicked;
        private Action _starterPackClicked;
        private Action _goldPassClicked;
        private Sprite _normalPlayButtonSprite;
        private Vector4 _normalPlayLevelMargin;

        private void Awake()
        {
            _starterPackButton ??=
                FindRequiredButton(StarterPackButtonName);
            _normalPlayButtonSprite = _playButton.image.sprite;
            _normalPlayLevelMargin = _playLevelText.margin;
            _playButton.onClick.AddListener(OnPlayButtonClicked);
            _settingButton.onClick.AddListener(OnSettingButtonClicked);
            _starterPackButton.onClick.AddListener(
                OnStarterPackButtonClicked);
            _goldPassButton.onClick.AddListener(OnGoldPassButtonClicked);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            _settingButton.onClick.RemoveListener(OnSettingButtonClicked);
            _starterPackButton.onClick.RemoveListener(
                OnStarterPackButtonClicked);
            _goldPassButton.onClick.RemoveListener(OnGoldPassButtonClicked);
            Clear();
        }

        public void SetActions(HomeViewActions actions)
        {
            _playClicked = actions.PlayClicked;
            _settingClicked = actions.SettingClicked;
            _starterPackClicked = actions.StarterPackClicked;
            _goldPassClicked = actions.GoldPassClicked;
            _resourceBarView.SetResourceClickActions(
                actions.CoinClicked,
                actions.HeartClicked);
        }

        public void SetPlayLevel(int levelNumber, LevelDifficulty difficulty)
        {
            _playLevelText.text = $"Level {levelNumber}";

            _playButton.image.sprite = difficulty switch
            {
                LevelDifficulty.Hard => _hardPlayButtonSprite,
                LevelDifficulty.SuperHard => _superHardPlayButtonSprite,
                _ => _normalPlayButtonSprite
            };

            _playLevelText.font = difficulty switch
            {
                LevelDifficulty.Hard => _hardPlayLevelFont,
                LevelDifficulty.SuperHard => _superHardPlayLevelFont,
                _ => _normalPlayLevelFont
            };

            Vector4 margin = _normalPlayLevelMargin;
            if (difficulty == LevelDifficulty.Hard ||
                difficulty == LevelDifficulty.SuperHard)
            {
                margin.y = 48f;
            }
            _playLevelText.margin = margin;
        }

        public void SetCoinBalance(long coinBalance)
        {
            _resourceBarView.SetCoinBalance(coinBalance);
        }

        public void SetHeartStatus(HeartStatus heartStatus)
        {
            _resourceBarView.SetHeartStatus(heartStatus);
        }

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            _resourceBarView.SetPlayerResources(coinBalance, heartStatus);
        }

        public void SetResourceClickActions(
            Action coinClicked,
            Action heartClicked)
        {
            _resourceBarView.SetResourceClickActions(
                coinClicked,
                heartClicked);
        }

        public CoinCounterView GetCoinCounter()
        {
            return _resourceBarView.CoinCounterView;
        }

        public void Clear()
        {
            _playClicked = null;
            _settingClicked = null;
            _starterPackClicked = null;
            _goldPassClicked = null;
            _resourceBarView.Clear();
        }

        private void OnPlayButtonClicked()
        {
            _playClicked?.Invoke();
        }

        private void OnSettingButtonClicked()
        {
            _settingClicked?.Invoke();
        }

        private Button FindRequiredButton(string buttonName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == buttonName)
                {
                    return buttons[i];
                }
            }

            throw new InvalidOperationException(
                $"{nameof(HomeView)} could not find {buttonName}.");
        }

        private void OnStarterPackButtonClicked()
        {
            _starterPackClicked?.Invoke();
        }

        private void OnGoldPassButtonClicked()
        {
            _goldPassClicked?.Invoke();
        }

    }
}
