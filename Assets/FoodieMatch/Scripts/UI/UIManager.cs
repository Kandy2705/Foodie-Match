using System;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.UI.Gameplay;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.LeaveGame;
using FoodieMatch.UI.Pause;
using FoodieMatch.UI.Popup;
using FoodieMatch.UI.Result;
using FoodieMatch.UI.Setting;
using UnityEngine;

namespace FoodieMatch.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private const string PopupShowSfxKey = "popup_show";

        [Header("Popup")]
        [SerializeField] private PopupManager _popupManager;

        [Header("HUD")]
        [SerializeField] private GameObject _gameplayHudPrefab;
        [SerializeField] private Transform _hudRoot;

        private IAudioService _audioService;
        private GameplayEvents _gameplayEvents;
        private GameObject _gameplayHud;
        private GameplayHudView _gameplayHudView;
        private bool _hasConstructed;

        public event Action PlayGameRequested;

        public event Action LeaveGameRequested;

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public void Construct(
            GameplayEvents gameplayEvents,
            IAudioService audioService)
        {
            if (_hasConstructed)
            {
                UnsubscribeEvents();
            }

            _audioService = audioService;

            _gameplayEvents = gameplayEvents;
            SubscribeEvents();

            _hasConstructed = true;

            if (_popupManager == null)
            {
                Debug.LogError("PopupManager is missing.");
            }
        }

        public void ShowHome()
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show home because PopupManager is missing.");
                return;
            }

            HomeView homeView = _popupManager.Show<HomeView>();

            if (homeView == null)
            {
                return;
            }

            homeView.SetActions(new HomeViewActions(OnHomePlayRequested, OnHomeSettingRequested));
        }

        public void HideHome()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<HomeView>();
        }

        public void ShowGameplayHud()
        {
            if (_gameplayHud != null)
            {
                _gameplayHud.SetActive(true);
                BindGameplayHudActions();
                return;
            }

            if (_gameplayHudPrefab == null || _hudRoot == null)
            {
                Debug.Log("Show Gameplay HUD");
                return;
            }

            _gameplayHud = Instantiate(_gameplayHudPrefab, _hudRoot);
            _gameplayHud.gameObject.name = _gameplayHudPrefab.gameObject.name;
            BindGameplayHudActions();
        }

        public void HideGameplayHud()
        {
            if (_gameplayHud == null)
            {
                return;
            }

            _gameplayHud.SetActive(false);
        }

        public void ShowSettingPopup()
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show setting popup because PopupManager is missing.");
                return;
            }

            SettingPopupView settingPopup = _popupManager.Show<SettingPopupView>();

            if (settingPopup == null)
            {
                return;
            }

            settingPopup.SetActions(
                new SettingPopupViewActions(
                    OnSettingCloseClicked,
                    OnSettingSoundChanged,
                    OnSettingMusicChanged));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HideSettingPopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<SettingPopupView>();
        }

        public void ShowPausePopup()
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show pause popup because PopupManager is missing.");
                return;
            }

            PauseView pauseView = _popupManager.Show<PauseView>();

            if (pauseView == null)
            {
                return;
            }

            pauseView.SetActions(
                new PauseViewActions(
                    OnPauseResumeClicked,
                    OnPauseRestartClicked,
                    OnPauseHomeClicked,
                    OnPauseCloseClicked));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HidePausePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<PauseView>();
        }

        public void ShowLeaveGamePopup()
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show leave game popup because PopupManager is missing.");
                return;
            }

            LeaveGamePopupView leaveGamePopup = _popupManager.Show<LeaveGamePopupView>();

            if (leaveGamePopup == null)
            {
                return;
            }

            leaveGamePopup.SetActions(
                new LeaveGamePopupViewActions(
                    OnLeaveGameCloseClicked,
                    OnLeaveGameLeaveClicked));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HideLeaveGamePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<LeaveGamePopupView>();
        }

        public void ShowWinPopup(
            Action claimCoinRewardClicked,
            Action doubleCoinRewardClicked)
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show win popup because PopupManager is missing.");
                return;
            }

            WinView winView = _popupManager.Show<WinView>();

            if (winView == null)
            {
                return;
            }

            winView.SetActions(
                new WinViewActions(
                    claimCoinRewardClicked,
                    doubleCoinRewardClicked));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HideWinPopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<WinView>();
        }

        public void ShowLosePopup(
            Action tryAgainClicked,
            Action homeClicked)
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show lose popup because PopupManager is missing.");
                return;
            }

            LoseView loseView = _popupManager.Show<LoseView>();

            if (loseView == null)
            {
                return;
            }

            loseView.SetActions(
                new LoseViewActions(
                    tryAgainClicked,
                    homeClicked));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HideLosePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<LoseView>();
        }

        public void HideAllPopups()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.HideAll();
        }

        private void BindGameplayHudActions()
        {
            if (_gameplayHud == null)
            {
                return;
            }

            if (_gameplayHudView == null)
            {
                _gameplayHudView = _gameplayHud.GetComponent<GameplayHudView>();

                if (_gameplayHudView == null)
                {
                    _gameplayHudView = _gameplayHud.AddComponent<GameplayHudView>();
                }
            }

            _gameplayHudView.SetActions(new GameplayHudViewActions(OnGameplayPauseRequested));
        }

        private void SubscribeEvents()
        {
            if (_gameplayEvents == null)
            {
                return;
            }

            _gameplayEvents.LevelStarted += OnLevelStarted;
            _gameplayEvents.LevelProgressChanged += OnLevelProgressChanged;
            _gameplayEvents.LevelEnded += OnLevelEnded;
        }

        private void UnsubscribeEvents()
        {
            if (_gameplayEvents == null)
            {
                return;
            }

            _gameplayEvents.LevelStarted -= OnLevelStarted;
            _gameplayEvents.LevelProgressChanged -= OnLevelProgressChanged;
            _gameplayEvents.LevelEnded -= OnLevelEnded;
        }

        private void OnHomePlayRequested()
        {
            PlayGameRequested?.Invoke();
        }

        private void OnHomeSettingRequested()
        {
            ShowSettingPopup();
        }

        private void OnGameplayPauseRequested()
        {
            ShowPausePopup();
        }

        private void OnSettingCloseClicked()
        {
            HideSettingPopup();
        }

        private void OnSettingSoundChanged(bool isOn)
        {
            Debug.Log($"Sound changed: {isOn}");
        }

        private void OnSettingMusicChanged(bool isOn)
        {
            Debug.Log($"Music changed: {isOn}");
        }

        private void OnPauseResumeClicked()
        {
            HidePausePopup();
        }

        private void OnPauseCloseClicked()
        {
            HidePausePopup();
        }

        private void OnPauseRestartClicked()
        {
            Debug.Log("Pause Restart Clicked");
            HidePausePopup();
        }

        private void OnPauseHomeClicked()
        {
            HidePausePopup();
            ShowLeaveGamePopup();
        }

        private void OnLeaveGameCloseClicked()
        {
            HideLeaveGamePopup();
            ShowPausePopup();
        }

        private void OnLeaveGameLeaveClicked()
        {
            HideAllPopups();
            LeaveGameRequested?.Invoke();
        }

        private void OnLevelStarted(LevelStartedEvent eventData)
        {
            Debug.Log($"Level Started: {eventData.LevelId}");
        }

        private void OnLevelProgressChanged(LevelProgressChangedEvent eventData)
        {
            Debug.Log($"Progress: {eventData.ServedCount}/{eventData.TotalCount}");
        }

        private void OnLevelEnded(LevelEndedEvent eventData)
        {
            Debug.Log($"Level Ended: {eventData.LevelId}, Win: {eventData.IsWin}, Reason: {eventData.Reason}");
        }
    }
}
