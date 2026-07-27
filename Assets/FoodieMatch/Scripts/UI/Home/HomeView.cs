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
        [Header("Actions")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingButton;
        [Header("Content")]
        [SerializeField] private TMP_Text _playLevelText;
        [SerializeField] private ResourceBarView _resourceBarView;
        [SerializeField] private Sprite _hardPlayButtonSprite;
        [SerializeField] private Sprite _superHardPlayButtonSprite;

        private Action _playClicked;
        private Action _settingClicked;
        private Sprite _normalPlayButtonSprite;

        private void Awake()
        {
            _normalPlayButtonSprite = _playButton.image.sprite;
            _playButton.onClick.AddListener(OnPlayButtonClicked);
            _settingButton.onClick.AddListener(OnSettingButtonClicked);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            _settingButton.onClick.RemoveListener(OnSettingButtonClicked);
            Clear();
        }

        public void SetActions(HomeViewActions actions)
        {
            _playClicked = actions.PlayClicked;
            _settingClicked = actions.SettingClicked;
            _resourceBarView.SetHeartClickAction(actions.HeartClicked);
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

        public CoinCounterView GetCoinCounter()
        {
            return _resourceBarView.CoinCounterView;
        }

        public void Clear()
        {
            _playClicked = null;
            _settingClicked = null;
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

    }
}
