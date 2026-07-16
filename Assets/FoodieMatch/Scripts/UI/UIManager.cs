using System;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Data.Booster;
using FoodieMatch.UI.BoosterGuide;
using FoodieMatch.UI.Gameplay;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.LeaveGame;
using FoodieMatch.UI.Pause;
using FoodieMatch.UI.Popup;
using FoodieMatch.UI.Result;
using FoodieMatch.UI.RetryGame;
using FoodieMatch.UI.Revive;
using FoodieMatch.UI.Setting;
using UnityEngine;

namespace FoodieMatch.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private const string PopupShowSfxKey = AudioKeys.SfxPopupShow;

        [Header("Popup")]
        [SerializeField] private PopupManager _popupManager;

        [Header("HUD")]
        [SerializeField] private GameObject _gameplayHudPrefab;
        [SerializeField] private Transform _hudRoot;

        [Header("Booster Guide")]
        [SerializeField] private BoosterGuideCatalogSO _boosterGuideCatalog;

        private IAudioService _audioService;
        private GameplayEvents _gameplayEvents;
        private GameObject _gameplayHud;
        private GameplayHudView _gameplayHudView;
        private bool _hasConstructed;
        private bool _returnToReviveOnLeaveClose;
        private int _currentLevelNumber = 1;
        private int _currentServedCount;
        private int _currentTotalCount;
        private BoosterType _currentBoosterGuideType;

        public event Action PlayGameRequested;

        public event Action LeaveGameRequested;

        public event Action RestartGameRequested;

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
            homeView.SetPlayLevelNumber(_currentLevelNumber);
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

            if (_audioService != null)
            {
                settingPopup.SetToggleStates(
                    _audioService.IsSfxEnabled,
                    _audioService.IsMusicEnabled);
            }

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
                    OnPauseCloseClicked,
                    OnSettingSoundChanged,
                    OnSettingMusicChanged));

            if (_audioService != null)
            {
                pauseView.SetToggleStates(
                    _audioService.IsSfxEnabled,
                    _audioService.IsMusicEnabled);
            }

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

        public void ShowRetryGamePopup()
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show retry game popup because PopupManager is missing.");
                return;
            }

            RetryGamePopupView retryGamePopup = _popupManager.Show<RetryGamePopupView>();

            if (retryGamePopup == null)
            {
                return;
            }

            retryGamePopup.SetActions(
                new RetryGamePopupViewActions(
                    OnRetryGameCloseClicked,
                    OnRetryGameRetryClicked));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HideRetryGamePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<RetryGamePopupView>();
        }

        public void ShowRevivePopup()
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show revive popup because PopupManager is missing.");
                return;
            }

            RevivePopupView revivePopup = _popupManager.Show<RevivePopupView>();

            if (revivePopup == null)
            {
                return;
            }

            revivePopup.SetActions(
                new RevivePopupViewActions(
                    OnReviveCloseClicked,
                    OnReviveFreeAdsClicked,
                    OnRevivePlayOnClicked));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HideRevivePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<RevivePopupView>();
        }

        public void ShowWinPopup(
            Action claimCoinRewardClicked,
            Action doubleCoinRewardClicked,
            string rewardAmountText = null,
            string rewardMultiplierText = null)
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

            if (!string.IsNullOrEmpty(rewardAmountText))
            {
                winView.SetRewardAmount(rewardAmountText);
            }

            if (!string.IsNullOrEmpty(rewardMultiplierText))
            {
                winView.SetRewardMultiplier(rewardMultiplierText);
            }

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

        public void ShowBoosterGuidePopup(BoosterType boosterType)
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show booster guide popup because PopupManager is missing.");
                return;
            }

            if (_boosterGuideCatalog == null)
            {
                Debug.LogError("Cannot show booster guide popup because BoosterGuideCatalog is missing.");
                return;
            }

            if (!_boosterGuideCatalog.TryGet(boosterType, out BoosterGuideContentEntry entry))
            {
                Debug.LogError($"Booster guide content not found for type: {boosterType}");
                return;
            }

            BoosterGuidePopupData popupData = BoosterGuidePopupData.FromCatalogEntry(entry);

            BoosterGuidePopupView popup = _popupManager.Show<BoosterGuidePopupView>(popupData);

            if (popup == null)
            {
                return;
            }

            _currentBoosterGuideType = boosterType;
            popup.SetActions(
                new BoosterGuidePopupViewActions(
                    OnBoosterGuideCloseClicked,
                    OnBoosterGuideFreeAdsClicked,
                    OnBoosterGuideBuyClicked));

            _audioService?.PlaySfx(PopupShowSfxKey);
        }

        public void HideBoosterGuidePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<BoosterGuidePopupView>();
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

            _gameplayHudView.SetActions(
                new GameplayHudViewActions(
                    OnGameplayPauseRequested,
                    OnGameplayBoosterRequested));
            _gameplayHudView.SetLevelNumber(_currentLevelNumber);
            _gameplayHudView.SetProgress(_currentServedCount, _currentTotalCount);
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

        private void OnGameplayBoosterRequested(int boosterIndex)
        {
            if (!BoosterGuideCatalogSO.TryFromButtonIndex(boosterIndex, out BoosterType boosterType))
            {
                Debug.LogWarning($"Unknown booster button index: {boosterIndex}");
                return;
            }

            ShowBoosterGuidePopup(boosterType);
        }

        private void OnBoosterGuideCloseClicked()
        {
            HideBoosterGuidePopup();
        }

        private void OnBoosterGuideFreeAdsClicked()
        {
            Debug.Log($"Booster guide free ads clicked: {_currentBoosterGuideType}");
            HideBoosterGuidePopup();
        }

        private void OnBoosterGuideBuyClicked()
        {
            Debug.Log($"Booster guide buy clicked: {_currentBoosterGuideType}");
            HideBoosterGuidePopup();
        }

        private void OnSettingCloseClicked()
        {
            HideSettingPopup();
        }

        private void OnSettingSoundChanged(bool isOn)
        {
            _audioService?.SetSfxEnabled(isOn);
        }

        private void OnSettingMusicChanged(bool isOn)
        {
            _audioService?.SetMusicEnabled(isOn);
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
            HidePausePopup();
            ShowRetryGamePopup();
        }

        private void OnPauseHomeClicked()
        {
            HidePausePopup();
            _returnToReviveOnLeaveClose = false;
            ShowLeaveGamePopup();
        }

        private void OnLeaveGameCloseClicked()
        {
            HideLeaveGamePopup();

            if (_returnToReviveOnLeaveClose)
            {
                _returnToReviveOnLeaveClose = false;
                ShowRevivePopup();
                return;
            }

            ShowPausePopup();
        }

        private void OnLeaveGameLeaveClicked()
        {
            _returnToReviveOnLeaveClose = false;
            HideAllPopups();
            LeaveGameRequested?.Invoke();
        }

        private void OnRetryGameCloseClicked()
        {
            HideRetryGamePopup();
            ShowPausePopup();
        }

        private void OnRetryGameRetryClicked()
        {
            HideAllPopups();
            RestartGameRequested?.Invoke();
        }

        private void OnReviveCloseClicked()
        {
            HideRevivePopup();
            _returnToReviveOnLeaveClose = true;
            ShowLeaveGamePopup();
        }

        private void OnReviveFreeAdsClicked()
        {
            Debug.Log("Revive Free Ads Clicked");
            HideRevivePopup();
        }

        private void OnRevivePlayOnClicked()
        {
            Debug.Log("Revive Play On Clicked");
            HideRevivePopup();
        }

        private void OnLevelStarted(LevelStartedEvent eventData)
        {
            _currentLevelNumber = eventData.LevelNumber;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.SetLevelNumber(eventData.LevelNumber);
            }

            Debug.Log($"Level Started: {eventData.LevelNumber}");
        }

        private void OnLevelProgressChanged(LevelProgressChangedEvent eventData)
        {
            _currentServedCount = eventData.ServedCount;
            _currentTotalCount = eventData.TotalCount;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.SetProgress(
                    eventData.ServedCount,
                    eventData.TotalCount);
            }

            Debug.Log($"Progress: {eventData.ServedCount}/{eventData.TotalCount}");
        }

        private void OnLevelEnded(LevelEndedEvent eventData)
        {
            Debug.Log($"Level Ended: {eventData.LevelNumber}, Win: {eventData.IsWin}, Reason: {eventData.Reason}");
        }
    }
}

