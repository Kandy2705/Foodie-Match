using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Data.Booster;
using FoodieMatch.UI.BoosterBuy;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Gameplay;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.LeaveGame;
using FoodieMatch.UI.Loading;
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
        [Header("Popup")]
        [SerializeField] private PopupManager _popupManager;

        [Header("HUD")]
        [SerializeField] private GameObject _gameplayHudPrefab;
        [SerializeField] private Transform _hudRoot;

        [Header("Loading")]
        [SerializeField] private LoadingScreenView _loadingScreenPrefab;
        [SerializeField] private Transform _loadingRoot;

        [Header("Booster Guide")]
        [SerializeField] private BoosterBuyCatalogSO _boosterBuyCatalog;

        private BoosterManager _boosterManager;
        private IAudioService _audioService;
        private GameplayEvents _gameplayEvents;
        private UiGlobalButtonClickSfx _uiGlobalButtonClickSfx;
        private GameObject _gameplayHud;
        private GameplayHudView _gameplayHudView;
        private LoadingScreenView _loadingScreenView;
        private bool _hasConstructed;
        private bool _returnToReviveOnLeaveClose;
        private Action _loseTryAgainClicked;
        private Action _loseHomeClicked;
        private int _currentLevelNumber = 1;
        private int _currentServedCount;
        private int _currentTotalCount;
        private int _currentComboCount;
        private float _currentComboFill;
        private BoosterType _currentBoosterBuyType;

        public event Action PlayGameRequested;

        public event Action LeaveGameRequested;

        public event Action RestartGameRequested;

        private void OnDestroy()
        {
            HideLoading();
            UnsubscribeEvents();
        }

        public void Construct(
            GameplayEvents gameplayEvents,
            IAudioService audioService,
            BoosterManager boosterManager = null)
        {
            if (_hasConstructed)
            {
                UnsubscribeEvents();
            }

            _audioService = audioService;
            EnsureGlobalButtonClickSfx(audioService);

            _gameplayEvents = gameplayEvents;
            _boosterManager = boosterManager;
            SubscribeEvents();

            _hasConstructed = true;

            if (_popupManager == null)
            {
                Debug.LogError("PopupManager is missing.");
            }
        }

        private void EnsureGlobalButtonClickSfx(IAudioService audioService)
        {
            if (_uiGlobalButtonClickSfx == null)
            {
                _uiGlobalButtonClickSfx = GetComponent<UiGlobalButtonClickSfx>();

                if (_uiGlobalButtonClickSfx == null)
                {
                    _uiGlobalButtonClickSfx =
                        gameObject.AddComponent<UiGlobalButtonClickSfx>();
                }
            }

            _uiGlobalButtonClickSfx.Construct(audioService);
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

        public void SetCurrentLevelNumber(int levelNumber)
        {
            _currentLevelNumber = levelNumber;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.SetLevelNumber(levelNumber);
            }
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

        public Task PlayLoadingAsync()
        {
            if (!TryGetLoadingScreen(out LoadingScreenView loadingScreenView))
            {
                return Task.CompletedTask;
            }

            return loadingScreenView.PlayAsync();
        }

        public void HideLoading()
        {
            _loadingScreenView?.Hide();
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
        }

        public void HideRetryGamePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<RetryGamePopupView>();
        }

        public void ShowRevivePopup(
            Action loseTryAgainClicked = null,
            Action loseHomeClicked = null)
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show revive popup because PopupManager is missing.");
                return;
            }

            if (loseTryAgainClicked != null)
            {
                _loseTryAgainClicked = loseTryAgainClicked;
            }

            if (loseHomeClicked != null)
            {
                _loseHomeClicked = loseHomeClicked;
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

            _audioService?.PlaySfx(AudioKeys.SfxWinGame);

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

            _audioService?.PlaySfx(AudioKeys.SfxLoseGame);

            loseView.SetActions(
                new LoseViewActions(
                    tryAgainClicked,
                    homeClicked));
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

        public void ShowBoosterBuyPopup(BoosterType boosterType)
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show booster guide popup because PopupManager is missing.");
                return;
            }

            if (_boosterBuyCatalog == null)
            {
                Debug.LogError("Cannot show booster buy popup because BoosterBuyCatalog is missing.");
                return;
            }

            if (!_boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
            {
                Debug.LogError($"Booster buy content not found for type: {boosterType}");
                return;
            }

            BoosterBuyPopupData popupData = BoosterBuyPopupData.FromCatalogEntry(entry);

            BoosterBuyPopupView popup = _popupManager.Show<BoosterBuyPopupView>(popupData);

            if (popup == null)
            {
                return;
            }

            _currentBoosterBuyType = boosterType;
            popup.SetActions(
                new BoosterBuyPopupViewActions(
                    OnBoosterBuyCloseClicked,
                    OnBoosterBuyFreeAdsClicked,
                    OnBoosterBuyBuyClicked));
        }

        public void HideBoosterBuyPopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<BoosterBuyPopupView>();
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
                    OnGameplayBoosterUseRequested,
                    OnGameplayBoosterAddRequested));
            _gameplayHudView.SetLevelNumber(_currentLevelNumber);
            _gameplayHudView.SetProgress(_currentServedCount, _currentTotalCount);
            _gameplayHudView.SetCombo(_currentComboCount, _currentComboFill);
            RefreshBoosterHud();
        }

        private bool TryGetLoadingScreen(out LoadingScreenView loadingScreenView)
        {
            if (_loadingScreenView != null)
            {
                loadingScreenView = _loadingScreenView;
                return true;
            }

            if (_loadingScreenPrefab == null || _loadingRoot == null)
            {
                Debug.LogError("Loading screen prefab or loading root is missing.");
                loadingScreenView = null;
                return false;
            }

            _loadingRoot.SetAsLastSibling();
            _loadingScreenView = Instantiate(_loadingScreenPrefab, _loadingRoot);
            _loadingScreenView.gameObject.name = _loadingScreenPrefab.gameObject.name;
            loadingScreenView = _loadingScreenView;
            return true;
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
            _gameplayEvents.ComboChanged += OnComboChanged;
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
            _gameplayEvents.ComboChanged -= OnComboChanged;
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

        private void OnGameplayBoosterUseRequested(int boosterIndex)
        {
            if (_boosterManager == null)
            {
                Debug.LogWarning("BoosterManager is not available.");
                return;
            }

            if (!BoosterBuyCatalogSO.TryFromButtonIndex(boosterIndex, out BoosterType boosterType))
            {
                Debug.LogWarning($"Unknown booster button index: {boosterIndex}");
                return;
            }

            if (_boosterManager.TryUse(boosterType))
            {
                Debug.Log($"Used {boosterType} booster. Remaining: {_boosterManager.GetCount(boosterType)}");
                RefreshBoosterHud();
            }
            else
            {
                Debug.Log($"No {boosterType} booster left.");
            }
        }

        private void OnGameplayBoosterAddRequested(int boosterIndex)
        {
            if (!BoosterBuyCatalogSO.TryFromButtonIndex(boosterIndex, out BoosterType boosterType))
            {
                Debug.LogWarning($"Unknown booster button index: {boosterIndex}");
                return;
            }

            ShowBoosterBuyPopup(boosterType);
        }

        private void OnBoosterBuyCloseClicked()
        {
            HideBoosterBuyPopup();
        }

        private void OnBoosterBuyFreeAdsClicked()
        {
            GrantBooster(_currentBoosterBuyType, 1);
            HideBoosterBuyPopup();
        }

        private void OnBoosterBuyBuyClicked()
        {
            GrantBooster(_currentBoosterBuyType, 1);
            HideBoosterBuyPopup();
        }

        private void GrantBooster(BoosterType type, int amount)
        {
            if (_boosterManager == null)
            {
                Debug.LogWarning("BoosterManager is not available.");
                return;
            }

            _boosterManager.Add(type, amount);
            Debug.Log($"Granted {amount}x {type} booster. Total: {_boosterManager.GetCount(type)}");
            RefreshBoosterHud();
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
                ShowRevivePopup();
                return;
            }

            ShowPausePopup();
        }

        private void OnLeaveGameLeaveClicked()
        {
            bool cameFromReviveFlow = _returnToReviveOnLeaveClose;
            _returnToReviveOnLeaveClose = false;

            if (cameFromReviveFlow)
            {
                HideLeaveGamePopup();

                if (_loseTryAgainClicked == null || _loseHomeClicked == null)
                {
                    Debug.LogError(
                        "Cannot show lose popup because lose actions are missing.");
                    LeaveGameRequested?.Invoke();
                    return;
                }

                ShowLosePopup(_loseTryAgainClicked, _loseHomeClicked);
                return;
            }

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
            _returnToReviveOnLeaveClose = false;
            HideRevivePopup();
        }

        private void OnRevivePlayOnClicked()
        {
            Debug.Log("Revive Play On Clicked");
            _returnToReviveOnLeaveClose = false;
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

        private void OnComboChanged(ComboChangedEvent eventData)
        {
            _currentComboCount = eventData.ComboCount;
            _currentComboFill = eventData.FillNormalized;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.SetCombo(
                    eventData.ComboCount,
                    eventData.FillNormalized);
            }
        }

        private void OnLevelEnded(LevelEndedEvent eventData)
        {
            Debug.Log($"Level Ended: {eventData.LevelNumber}, Win: {eventData.IsWin}, Reason: {eventData.Reason}");
        }

        private void RefreshBoosterHud()
        {
            if (_gameplayHudView == null || _boosterManager == null)
            {
                return;
            }

            _gameplayHudView.SetBoosterCounts(_boosterManager.GetCounts());
        }
    }
}

