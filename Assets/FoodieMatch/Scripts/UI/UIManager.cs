using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Audio;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.UI.Advertising;
using FoodieMatch.UI.Booster;
using FoodieMatch.UI.BoosterBuy;
using FoodieMatch.UI.BoosterGuide;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Gameplay;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.LeaveGame;
using FoodieMatch.UI.Loading;
using FoodieMatch.UI.MainMenu;
using FoodieMatch.UI.Pause;
using FoodieMatch.UI.Popup;
using FoodieMatch.UI.Reward;
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
        [SerializeField] private UiGlobalButtonClickSfx _uiGlobalButtonClickSfx;

        [Header("HUD")]
        [SerializeField] private GameplayHudView _gameplayHudPrefab;
        [SerializeField] private Transform _hudRoot;

        [Header("Loading")]
        [SerializeField] private LoadingScreenView _loadingScreenPrefab;
        [SerializeField] private Transform _loadingRoot;

        [Header("Reward Effect")]
        [SerializeField] private CoinRewardOverlayView _coinRewardOverlayPrefab;
        [SerializeField] private Transform _rewardEffectRoot;

        [Header("Booster Guide")]
        [SerializeField] private BoosterBuyCatalogSO _boosterBuyCatalog;

        private readonly List<BoosterBuyContentEntry> _pendingBoosterGuides = new();

        private BoosterManager _boosterManager;
        private IGameBoosterConfig _boosterConfig;
        private IGameEconomyConfig _economyConfig;
        private PlayerProfileService _playerProfileService;
        private IAudioService _audioService;
        private GameplayEvents _gameplayEvents;
        private GameplayHudView _gameplayHudView;
        private LoadingScreenView _loadingScreenView;
        private CoinRewardOverlayView _coinRewardOverlayView;
        private bool _returnToReviveOnLeaveClose;
        private bool _isBoosterGuideShowing;
        private Action _loseTryAgainClicked;
        private Action _loseHomeClicked;
        private Action _pendingBoxRescueCallback;
        private Action _pendingLoseConfirmedCallback;
        private int _currentLevelNumber = 1;
        private int _currentServedCount;
        private int _currentTotalCount;
        private int _currentComboCount;
        private float _currentComboRemainingSeconds;
        private BoosterType _currentBoosterBuyType;
        private BoosterType _currentBoosterGuideType;
        private int _pendingUnlockSlotIndex = -1;
        private Action<int> _pendingUnlockCallback;

        public event Action PlayGameRequested;

        public event Action LeaveGameRequested;

        public event Action<BoosterType> BoosterCoinPurchaseRequested;

        public event Action<BoosterType> BoosterRewardedAdRequested;

        public Func<BoosterType, bool> BoosterUseHandler { get; set; }

        public Func<bool> RestartGameHandler { get; set; }

        private void OnDestroy()
        {
            CompleteCoinRewardImmediately();
            HideLoading();
            UnsubscribeEvents();
        }

        public void Construct(
            GameplayEvents gameplayEvents,
            IAudioService audioService,
            BoosterManager boosterManager,
            IGameBoosterConfig boosterConfig,
            IGameEconomyConfig economyConfig,
            PlayerProfileService playerProfileService)
        {
            _audioService = audioService;
            _uiGlobalButtonClickSfx.Construct(audioService);

            _gameplayEvents = gameplayEvents;
            _boosterManager = boosterManager;
            _boosterConfig = boosterConfig;
            _economyConfig = economyConfig;
            _playerProfileService = playerProfileService;
            SubscribeEvents();
        }

        public void ShowHome(long displayedCoinBalance)
        {
            CompleteCoinRewardImmediately();

            MainMenuView mainMenuView = _popupManager.Show<MainMenuView>();

            if (!mainMenuView.TryGetView<HomeView>(out HomeView homeView))
            {
                Debug.LogError("HomeView is not registered in MainMenuView.", mainMenuView);
                return;
            }

            homeView.SetActions(
                new HomeViewActions(
                    OnHomePlayRequested,
                    OnHomeSettingRequested));

            homeView.SetPlayLevelNumber(_currentLevelNumber);

            homeView.SetPlayerResources(displayedCoinBalance, _playerProfileService.GetHeartStatus());
        }

        public void PlayHomeCoinReward(
            long startingCoinBalance,
            long targetCoinBalance,
            int coinValuePerImage)
        {
            if (!_popupManager.TryGetOpened(out MainMenuView mainMenuView))
            {
                return;
            }

            if (!mainMenuView.TryGetView<HomeView>(out HomeView homeView))
            {
                return;
            }

            PlayCoinReward(
                homeView.GetCoinCounter(),
                spawnPoint: null,
                startingCoinBalance,
                targetCoinBalance,
                coinValuePerImage,
                OnHomeCoinArrived);
        }

        public void PlayCoinReward(
            CoinCounterView coinCounter,
            RectTransform spawnPoint,
            long startingCoinBalance,
            long targetCoinBalance,
            int coinValuePerImage,
            Action coinArrived)
        {
            CoinRewardOverlayView coinRewardOverlay = GetOrCreateCoinRewardOverlay();
            coinRewardOverlay.PlayCoinReward(
                coinCounter,
                spawnPoint,
                startingCoinBalance,
                targetCoinBalance,
                coinValuePerImage,
                coinArrived);
        }

        public void CompleteCoinRewardImmediately()
        {
            _coinRewardOverlayView?.CompleteRewardImmediately();
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
            CompleteCoinRewardImmediately();

            _popupManager.Hide<MainMenuView>();
        }

        public void ShowGameplayHud()
        {
            if (_gameplayHudView != null)
            {
                _gameplayHudView.gameObject.SetActive(true);
                BindGameplayHudActions();
                return;
            }

            _gameplayHudView = Instantiate(_gameplayHudPrefab, _hudRoot);
            _gameplayHudView.gameObject.name = _gameplayHudPrefab.gameObject.name;
            BindGameplayHudActions();
        }

        public void HideGameplayHud()
        {
            if (_gameplayHudView == null)
            {
                return;
            }

            _gameplayHudView.gameObject.SetActive(false);
        }

        public void ShowComboFeedback(Vector3 worldPosition)
        {
            _gameplayHudView?.ShowComboFeedback(worldPosition);
        }

        public Task PlayLoadingAsync()
        {
            return GetOrCreateLoadingScreen().PlayAsync();
        }

        public void HideLoading()
        {
            _loadingScreenView?.Hide();
            TryShowNextBoosterGuide();
        }

        public void ShowSettingPopup()
        {
            SettingPopupView settingPopup = _popupManager.Show<SettingPopupView>();
            settingPopup.SetActions(
                new SettingPopupViewActions(
                    OnSettingCloseClicked,
                    OnSettingSoundChanged,
                    OnSettingMusicChanged));

            settingPopup.SetToggleStates(
                _audioService.IsSfxEnabled,
                _audioService.IsMusicEnabled);
        }

        public void HideSettingPopup()
        {
            _popupManager.Hide<SettingPopupView>();
        }

        public void ShowPausePopup()
        {
            PauseView pauseView = _popupManager.Show<PauseView>();
            pauseView.SetActions(
                new PauseViewActions(
                    OnPauseResumeClicked,
                    OnPauseRestartClicked,
                    OnPauseHomeClicked,
                    OnPauseCloseClicked,
                    OnSettingSoundChanged,
                    OnSettingMusicChanged));

            pauseView.SetToggleStates(
                _audioService.IsSfxEnabled,
                _audioService.IsMusicEnabled);
        }

        public void HidePausePopup()
        {
            _popupManager.Hide<PauseView>();
        }

        public void ShowLeaveGamePopup()
        {
            LeaveGamePopupView leaveGamePopup = _popupManager.Show<LeaveGamePopupView>();
            leaveGamePopup.SetActions(
                new LeaveGamePopupViewActions(
                    OnLeaveGameCloseClicked,
                    OnLeaveGameLeaveClicked));
        }

        public void HideLeaveGamePopup()
        {
            _popupManager.Hide<LeaveGamePopupView>();
        }

        public void ShowRetryGamePopup()
        {
            RetryGamePopupView retryGamePopup = _popupManager.Show<RetryGamePopupView>();
            retryGamePopup.SetActions(
                new RetryGamePopupViewActions(
                    OnRetryGameCloseClicked,
                    OnRetryGameRetryClicked));
        }

        public void HideRetryGamePopup()
        {
            _popupManager.Hide<RetryGamePopupView>();
        }

        public void ShowRevivePopup(
            Action loseTryAgainClicked = null,
            Action loseHomeClicked = null,
            Action onBoxRescueConfirmed = null,
            Action onLoseConfirmed = null)
        {
            if (loseTryAgainClicked != null)
            {
                _loseTryAgainClicked = loseTryAgainClicked;
            }

            if (loseHomeClicked != null)
            {
                _loseHomeClicked = loseHomeClicked;
            }

            if (onBoxRescueConfirmed != null)
            {
                _pendingBoxRescueCallback = onBoxRescueConfirmed;
            }

            if (onLoseConfirmed != null)
            {
                _pendingLoseConfirmedCallback = onLoseConfirmed;
            }

            RevivePopupView revivePopup = _popupManager.Show<RevivePopupView>();
            revivePopup.SetActions(
                new RevivePopupViewActions(
                    OnReviveCloseClicked,
                    OnReviveFreeAdsClicked,
                    OnRevivePlayOnClicked));
            SetCurrentPlayerResources(revivePopup);
        }

        public void HideRevivePopup()
        {
            _popupManager.Hide<RevivePopupView>();
        }

        public void ShowWinPopup(
            Action claimCoinRewardClicked,
            Action doubleCoinRewardClicked,
            long regularRewardAmount,
            long doubleRewardAmount)
        {
            WinView winView = _popupManager.Show<WinView>();
            _audioService.PlaySfx(AudioKeys.SfxWinGame);

            winView.SetActions(
                new WinViewActions(
                    claimCoinRewardClicked,
                    doubleCoinRewardClicked));
            winView.SetRewardAmounts(regularRewardAmount, doubleRewardAmount);
        }

        public void HideWinPopup()
        {
            _popupManager.Hide<WinView>();
        }

        public bool ShowFakeRewardedAdPopup(
            Action completed,
            Action cancelled)
        {
            FakeRewardedAdPopupView popup =
                _popupManager.Show<FakeRewardedAdPopupView>();
            popup.SetActions(
                new FakeRewardedAdPopupViewActions(
                    completed,
                    cancelled));
            return true;
        }

        public void HideFakeRewardedAdPopup()
        {
            _popupManager.Hide<FakeRewardedAdPopupView>();
        }

        public void ShowLosePopup(
            Action tryAgainClicked,
            Action homeClicked)
        {
            LoseView loseView = _popupManager.Show<LoseView>();
            _audioService.PlaySfx(AudioKeys.SfxLoseGame);

            loseView.SetActions(
                new LoseViewActions(
                    tryAgainClicked,
                    homeClicked));
            SetCurrentPlayerResources(loseView);
        }

        public void HideLosePopup()
        {
            _popupManager.Hide<LoseView>();
        }

        public BoosterSwapPopup ShowSwapPopup()
        {
            return _popupManager.Show<BoosterSwapPopup>();
        }

        public void HideSwapPopup()
        {
            _popupManager.Hide<BoosterSwapPopup>();
        }

        public void HideAllPopups()
        {
            CompleteCoinRewardImmediately();
            _pendingBoosterGuides.Clear();
            _isBoosterGuideShowing = false;

            _popupManager.HideAll();
        }

        public void ShowBoosterBuyPopup(BoosterType boosterType)
        {
            if (!_boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
            {
                Debug.LogError($"Booster buy content not found for type: {boosterType}");
                return;
            }

            int coinPrice = _economyConfig.GetBoosterPrice(boosterType);
            BoosterBuyPopupData popupData =
                BoosterBuyPopupData.FromCatalogEntry(
                    entry,
                    coinPrice.ToString());

            BoosterBuyPopupView popup = _popupManager.Show<BoosterBuyPopupView>(popupData);
            _currentBoosterBuyType = boosterType;
            popup.SetActions(
                new BoosterBuyPopupViewActions(
                    OnBoosterBuyCloseClicked,
                    OnBoosterBuyFreeAdsClicked,
                    OnBoosterBuyBuyClicked));
            SetCurrentPlayerResources(popup);
        }

        public void HideBoosterBuyPopup()
        {
            _popupManager.Hide<BoosterBuyPopupView>();
        }

        public void RefreshBoosterInventory()
        {
            RefreshBoosterHud();
        }

        public void RefreshOpenedResourceBars()
        {
            long coinBalance = _playerProfileService.CoinBalance;

            HeartStatus heartStatus =
                _playerProfileService.GetHeartStatus();

            if (_popupManager.TryGetOpened(out MainMenuView mainMenuView) &&
                mainMenuView.TryGetView<HomeView>(out HomeView homeView))
            {
                homeView.SetPlayerResources(coinBalance, heartStatus);
            }

            if (_popupManager.TryGetOpened(out BoosterBuyPopupView boosterBuyPopup))
            {
                boosterBuyPopup.SetPlayerResources(coinBalance, heartStatus);
            }

            if (_popupManager.TryGetOpened(out RevivePopupView revivePopup))
            {
                revivePopup.SetPlayerResources(coinBalance, heartStatus);
            }

            if (_popupManager.TryGetOpened(out LoseView loseView))
            {
                loseView.SetPlayerResources(coinBalance, heartStatus);
            }
        }

        public void ShowUnlockLockedPackagePopup(int slotIndex, Action<int> onUnlockConfirmed)
        {
            if (!_boosterBuyCatalog.TryGet(BoosterType.Box, out BoosterBuyContentEntry entry))
            {
                Debug.LogError("Booster buy content not found for Box.");
                return;
            }

            BoosterBuyPopupData popupData = BoosterBuyPopupData.FromCatalogEntry(entry);
            BoosterBuyPopupView popup = _popupManager.Show<BoosterBuyPopupView>(popupData);
            _pendingUnlockSlotIndex = slotIndex;
            _pendingUnlockCallback = onUnlockConfirmed;

            popup.SetActions(
                new BoosterBuyPopupViewActions(
                    OnUnlockPopupCloseClicked,
                    OnUnlockPopupBuyClicked,
                    OnUnlockPopupBuyClicked));
        }

        private void OnUnlockPopupCloseClicked()
        {
            _pendingUnlockSlotIndex = -1;
            _pendingUnlockCallback = null;
            HideBoosterBuyPopup();
        }

        private void OnUnlockPopupBuyClicked()
        {
            int slotIndex = _pendingUnlockSlotIndex;
            Action<int> callback = _pendingUnlockCallback;
            _pendingUnlockSlotIndex = -1;
            _pendingUnlockCallback = null;
            HideBoosterBuyPopup();

            if (slotIndex >= 0 && callback != null)
            {
                callback.Invoke(slotIndex);
            }
        }

        public void ShowBoosterGuidePopup(BoosterType boosterType)
        {
            if (!_boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
            {
                Debug.LogError($"Booster guide content not found for type: {boosterType}");
                return;
            }

            BoosterGuidePopupData popupData = BoosterGuidePopupData.FromCatalogEntry(entry);
            BoosterGuidePopupView popup = _popupManager.Show<BoosterGuidePopupView>(popupData);
            _currentBoosterGuideType = boosterType;
            _isBoosterGuideShowing = true;
            popup.SetActions(
                new BoosterGuidePopupViewActions(
                    OnBoosterGuideClosed));
        }

        public void HideBoosterGuidePopup()
        {
            _popupManager.Hide<BoosterGuidePopupView>();
            _isBoosterGuideShowing = false;
        }

        private void BindGameplayHudActions()
        {
            _gameplayHudView.SetActions(
                new GameplayHudViewActions(
                    OnGameplayPauseRequested,
                    OnGameplayBoosterUseRequested,
                    OnGameplayBoosterAddRequested));
            _gameplayHudView.SetLevelNumber(_currentLevelNumber);
            _gameplayHudView.SetProgress(_currentServedCount, _currentTotalCount);
            _gameplayHudView.SetCombo(_currentComboCount, _currentComboRemainingSeconds);
            RefreshBoosterHud();
        }

        private CoinRewardOverlayView GetOrCreateCoinRewardOverlay()
        {
            if (_coinRewardOverlayView != null)
            {
                return _coinRewardOverlayView;
            }

            _coinRewardOverlayView = Instantiate(_coinRewardOverlayPrefab, _rewardEffectRoot);
            _coinRewardOverlayView.gameObject.name = _coinRewardOverlayPrefab.gameObject.name;
            return _coinRewardOverlayView;
        }

        private LoadingScreenView GetOrCreateLoadingScreen()
        {
            if (_loadingScreenView != null)
            {
                return _loadingScreenView;
            }

            _loadingRoot.SetAsLastSibling();
            _loadingScreenView = Instantiate(_loadingScreenPrefab, _loadingRoot);
            _loadingScreenView.gameObject.name = _loadingScreenPrefab.gameObject.name;
            return _loadingScreenView;
        }

        private void SubscribeEvents()
        {
            _gameplayEvents.LevelStarted += OnLevelStarted;
            _gameplayEvents.LevelProgressChanged += OnLevelProgressChanged;
            _gameplayEvents.ComboChanged += OnComboChanged;
        }

        private void UnsubscribeEvents()
        {
            _gameplayEvents.LevelStarted -= OnLevelStarted;
            _gameplayEvents.LevelProgressChanged -= OnLevelProgressChanged;
            _gameplayEvents.ComboChanged -= OnComboChanged;
        }

        private void OnHomePlayRequested()
        {
            CompleteCoinRewardImmediately();
            PlayGameRequested?.Invoke();
        }

        private void OnHomeSettingRequested()
        {
            ShowSettingPopup();
        }

        private void SetCurrentPlayerResources(
            IPlayerResourceView resourceView)
        {
            resourceView.SetPlayerResources(_playerProfileService.CoinBalance, _playerProfileService.GetHeartStatus());
        }

        private void OnHomeCoinArrived()
        {
            _audioService.PlaySfx(AudioKeys.SfxCoinReceive);
        }

        private void OnGameplayPauseRequested()
        {
            ShowPausePopup();
        }

        private void OnGameplayBoosterUseRequested(int boosterIndex)
        {
            if (!BoosterBuyCatalogSO.TryFromButtonIndex(boosterIndex, out BoosterType boosterType))
            {
                Debug.LogWarning($"Unknown booster button index: {boosterIndex}");
                return;
            }

            if (!IsBoosterUnlocked(boosterType))
            {
                Debug.Log($"Booster {boosterType} is locked until a later level.");
                return;
            }

            if (!_boosterManager.HasCount(boosterType))
            {
                Debug.Log($"No {boosterType} booster left.");
                return;
            }

            if (BoosterUseHandler == null || !BoosterUseHandler.Invoke(boosterType))
            {
                Debug.Log($"Booster {boosterType} could not be applied.");
                return;
            }

            Debug.Log($"Used {boosterType} booster. Remaining: {_boosterManager.GetCount(boosterType)}");
            RefreshBoosterHud();
        }

        private void OnGameplayBoosterAddRequested(int boosterIndex)
        {
            if (!BoosterBuyCatalogSO.TryFromButtonIndex(boosterIndex, out BoosterType boosterType))
            {
                Debug.LogWarning($"Unknown booster button index: {boosterIndex}");
                return;
            }

            if (!IsBoosterUnlocked(boosterType))
            {
                Debug.Log($"Booster {boosterType} is locked until a later level.");
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
            BoosterRewardedAdRequested?.Invoke(_currentBoosterBuyType);
        }

        private void OnBoosterBuyBuyClicked()
        {
            BoosterCoinPurchaseRequested?.Invoke(_currentBoosterBuyType);
        }

        private void OnSettingCloseClicked()
        {
            HideSettingPopup();
        }

        private void OnSettingSoundChanged(bool isOn)
        {
            _audioService.SetSfxEnabled(isOn);
        }

        private void OnSettingMusicChanged(bool isOn)
        {
            _audioService.SetMusicEnabled(isOn);
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
                ShowRevivePopup(
                    onBoxRescueConfirmed:
                        _pendingBoxRescueCallback,
                    onLoseConfirmed:
                        _pendingLoseConfirmedCallback);

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

                if (_loseTryAgainClicked == null ||
                    _loseHomeClicked == null ||
                    _pendingLoseConfirmedCallback == null)
                {
                    Debug.LogError(
                        "Cannot show lose popup because lose callbacks are missing.");
                    LeaveGameRequested?.Invoke();
                    return;
                }

                Action loseConfirmedCallback =
                    _pendingLoseConfirmedCallback;
                _pendingBoxRescueCallback = null;
                _pendingLoseConfirmedCallback = null;
                loseConfirmedCallback.Invoke();
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
            if (RestartGameHandler == null ||
                !RestartGameHandler.Invoke())
            {
                return;
            }

            HideAllPopups();
        }

        private void OnReviveCloseClicked()
        {
            HideRevivePopup();
            _returnToReviveOnLeaveClose = true;
            ShowLeaveGamePopup();
        }

        private void OnReviveFreeAdsClicked()
        {
            ConfirmBoxRescue();
        }

        private void OnRevivePlayOnClicked()
        {
            Debug.Log("Revive Play On Clicked");
            ConfirmBoxRescue();
        }

        private void ConfirmBoxRescue()
        {
            Action callback = _pendingBoxRescueCallback;

            if (callback == null)
            {
                Debug.LogError("Box rescue callback is missing.");
                return;
            }

            _pendingBoxRescueCallback = null;
            _pendingLoseConfirmedCallback = null;
            _returnToReviveOnLeaveClose = false;

            HideRevivePopup();

            callback.Invoke();
        }

        private void OnLevelStarted(LevelStartedEvent eventData)
        {
            _currentLevelNumber = eventData.LevelNumber;
            _currentComboCount = 0;
            _currentComboRemainingSeconds = 0f;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.SetLevelNumber(eventData.LevelNumber);
                _gameplayHudView.ResetCombo();
            }

            RefreshBoosterHud();
            TryQueueBoosterGuidesForLevel(eventData.LevelNumber);
            Debug.Log($"Level Started: {eventData.LevelNumber}");
        }

        private void TryQueueBoosterGuidesForLevel(int levelNumber)
        {
            _pendingBoosterGuides.Clear();

            IReadOnlyList<BoosterBuyContentEntry> entries =
                _boosterBuyCatalog.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                BoosterBuyContentEntry entry = entries[i];

                if (entry == null ||
                    GetBoosterUnlockLevel(entry.BoosterType) != levelNumber ||
                    HasSeenBoosterGuide(entry.BoosterType))
                {
                    continue;
                }

                _pendingBoosterGuides.Add(entry);
            }
        }

        private void TryShowNextBoosterGuide()
        {
            if (_isBoosterGuideShowing || _pendingBoosterGuides.Count == 0)
            {
                return;
            }

            BoosterBuyContentEntry entry = _pendingBoosterGuides[0];
            _pendingBoosterGuides.RemoveAt(0);

            if (entry == null)
            {
                TryShowNextBoosterGuide();
                return;
            }

            ShowBoosterGuidePopup(entry.BoosterType);
        }

        private void OnBoosterGuideClosed()
        {
            MarkBoosterGuideSeen(_currentBoosterGuideType);
            HideBoosterGuidePopup();
            TryShowNextBoosterGuide();
        }

        private bool HasSeenBoosterGuide(BoosterType boosterType)
        {
            return _boosterManager.HasSeenGuide(boosterType);
        }

        private void MarkBoosterGuideSeen(BoosterType boosterType)
        {
            _boosterManager.MarkGuideSeen(boosterType);
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

        }

        private void OnComboChanged(ComboChangedEvent eventData)
        {
            _currentComboCount = eventData.ComboCount;
            _currentComboRemainingSeconds = eventData.RemainingSeconds;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.SetCombo(
                    eventData.ComboCount,
                    eventData.RemainingSeconds);
            }
        }

        private void RefreshBoosterHud()
        {
            if (_gameplayHudView == null)
            {
                return;
            }

            _gameplayHudView.SetBoosterCounts(_boosterManager.GetCounts());
            RefreshBoosterUnlockStates();
        }

        private void RefreshBoosterUnlockStates()
        {
            if (_gameplayHudView == null)
            {
                return;
            }

            bool[] unlockedStates = new bool[5];

            for (int i = 0; i < unlockedStates.Length; i++)
            {
                if (!BoosterBuyCatalogSO.TryFromButtonIndex(i, out BoosterType boosterType))
                {
                    unlockedStates[i] = false;
                    continue;
                }

                int unlockLevel = GetBoosterUnlockLevel(boosterType);
                unlockedStates[i] = _currentLevelNumber >= unlockLevel;
                _gameplayHudView.SetBoosterUnlockLevel(i, unlockLevel);

                if (_boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
                {
                    _gameplayHudView.SetBoosterLockedSprites(
                        i,
                        _boosterBuyCatalog.LockedButtonSprite,
                        entry.LockedIconSprite);
                }
            }

            _gameplayHudView.SetBoosterUnlockedStates(unlockedStates);
        }

        private bool IsBoosterUnlocked(BoosterType boosterType)
        {
            return _currentLevelNumber >= GetBoosterUnlockLevel(boosterType);
        }

        private int GetBoosterUnlockLevel(BoosterType boosterType)
        {
            return _boosterConfig.GetUnlockLevel(boosterType);
        }
    }
}

