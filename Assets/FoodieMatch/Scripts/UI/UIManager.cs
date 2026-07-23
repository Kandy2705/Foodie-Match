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
using FoodieMatch.Data.Booster;
using FoodieMatch.UI.Advertising;
using FoodieMatch.UI.Booster;
using FoodieMatch.UI.BoosterBuy;
using FoodieMatch.UI.BoosterGuide;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Gameplay;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.LeaveGame;
using FoodieMatch.UI.Loading;
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

        [Header("HUD")]
        [SerializeField] private GameObject _gameplayHudPrefab;
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
        private UiGlobalButtonClickSfx _uiGlobalButtonClickSfx;
        private GameObject _gameplayHud;
        private GameplayHudView _gameplayHudView;
        private LoadingScreenView _loadingScreenView;
        private CoinRewardOverlayView _coinRewardOverlayView;
        private bool _hasConstructed;
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
            if (_hasConstructed)
            {
                UnsubscribeEvents();
            }

            _audioService = audioService;
            EnsureGlobalButtonClickSfx(audioService);

            _gameplayEvents = gameplayEvents;
            _boosterManager = boosterManager;
            _boosterConfig = boosterConfig ??
                throw new ArgumentNullException(nameof(boosterConfig));
            _economyConfig = economyConfig;
            _playerProfileService = playerProfileService;
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

        public void ShowHome(long displayedCoinBalance)
        {
            CompleteCoinRewardImmediately();

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

            homeView.SetActions(
                new HomeViewActions(
                    OnHomePlayRequested,
                    OnHomeSettingRequested));
            homeView.SetPlayLevelNumber(_currentLevelNumber);
            homeView.SetPlayerResources(
                displayedCoinBalance,
                _playerProfileService.GetHeartStatus());
        }

        public void PlayHomeCoinReward(
            long startingCoinBalance,
            long targetCoinBalance,
            int coinValuePerImage)
        {
            if (_popupManager == null)
            {
                return;
            }

            if (!_popupManager.TryGetOpened(out HomeView homeView))
            {
                return;
            }

            CoinCounterView coinCounter = homeView.GetCoinCounter();

            if (coinCounter == null)
            {
                Debug.LogError("Cannot play home coin reward because CoinCounterView is missing.");
                return;
            }

            PlayCoinReward(
                coinCounter,
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

            if (coinRewardOverlay == null)
            {
                coinCounter?.SetCoinBalance(targetCoinBalance);
                return;
            }

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

        public void ShowComboFeedback(Vector3 worldPosition)
        {
            _gameplayHudView?.ShowComboFeedback(worldPosition);
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
            TryShowNextBoosterGuide();
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
            Action loseHomeClicked = null,
            Action onBoxRescueConfirmed = null,
            Action onLoseConfirmed = null)
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

            if (onBoxRescueConfirmed != null)
            {
                _pendingBoxRescueCallback = onBoxRescueConfirmed;
            }

            if (onLoseConfirmed != null)
            {
                _pendingLoseConfirmedCallback = onLoseConfirmed;
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
            SetCurrentPlayerResources(revivePopup);
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
            long regularRewardAmount,
            long doubleRewardAmount)
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
            winView.SetRewardAmounts(regularRewardAmount, doubleRewardAmount);
        }

        public void HideWinPopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<WinView>();
        }

        public bool ShowFakeRewardedAdPopup(
            Action completed,
            Action cancelled)
        {
            if (_popupManager == null)
            {
                Debug.LogError(
                    "Cannot show fake rewarded ad because PopupManager is missing.");
                return false;
            }

            FakeRewardedAdPopupView popup =
                _popupManager.Show<FakeRewardedAdPopupView>();

            if (popup == null)
            {
                return false;
            }

            popup.SetActions(
                new FakeRewardedAdPopupViewActions(
                    completed,
                    cancelled));
            return true;
        }

        public void HideFakeRewardedAdPopup()
        {
            _popupManager?.Hide<FakeRewardedAdPopupView>();
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
            SetCurrentPlayerResources(loseView);
        }

        public void HideLosePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<LoseView>();
        }

        public BoosterSwapPopup ShowSwapPopup()
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show swap popup because PopupManager is missing.");
                return null;
            }

            return _popupManager.Show<BoosterSwapPopup>();
        }

        public void HideSwapPopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<BoosterSwapPopup>();
        }

        public void HideAllPopups()
        {
            CompleteCoinRewardImmediately();
            _pendingBoosterGuides.Clear();
            _isBoosterGuideShowing = false;

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

            if (_economyConfig == null)
            {
                Debug.LogError("Cannot show booster buy popup because GameEconomyConfig is missing.");
                return;
            }

            int coinPrice = _economyConfig.GetBoosterPrice(boosterType);
            BoosterBuyPopupData popupData =
                BoosterBuyPopupData.FromCatalogEntry(
                    entry,
                    coinPrice.ToString());

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
            SetCurrentPlayerResources(popup);
        }

        public void HideBoosterBuyPopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<BoosterBuyPopupView>();
        }

        public void RefreshBoosterInventory()
        {
            RefreshBoosterHud();
        }

        public void RefreshOpenedResourceBars()
        {
            if (_playerProfileService == null ||
                _popupManager == null)
            {
                return;
            }

            long coinBalance = _playerProfileService.CoinBalance;
            HeartStatus heartStatus =
                _playerProfileService.GetHeartStatus();

            if (_popupManager.TryGetOpened(out HomeView homeView))
            {
                homeView.SetPlayerResources(
                    coinBalance,
                    heartStatus);
            }

            if (_popupManager.TryGetOpened(
                    out BoosterBuyPopupView boosterBuyPopup))
            {
                boosterBuyPopup.SetPlayerResources(
                    coinBalance,
                    heartStatus);
            }

            if (_popupManager.TryGetOpened(
                    out RevivePopupView revivePopup))
            {
                revivePopup.SetPlayerResources(
                    coinBalance,
                    heartStatus);
            }

            if (_popupManager.TryGetOpened(out LoseView loseView))
            {
                loseView.SetPlayerResources(
                    coinBalance,
                    heartStatus);
            }
        }
        
        public void ShowUnlockLockedPackagePopup(int slotIndex, Action<int> onUnlockConfirmed)
        {
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show unlock popup because PopupManager is missing.");
                return;
            }

            if (_boosterBuyCatalog == null)
            {
                Debug.LogError("Cannot show unlock popup because BoosterBuyCatalog is missing.");
                return;
            }

            if (!_boosterBuyCatalog.TryGet(BoosterType.Box, out BoosterBuyContentEntry entry))
            {
                Debug.LogError("Booster buy content not found for Box.");
                return;
            }

            BoosterBuyPopupData popupData = BoosterBuyPopupData.FromCatalogEntry(entry);
            BoosterBuyPopupView popup = _popupManager.Show<BoosterBuyPopupView>(popupData);

            if (popup == null)
            {
                return;
            }

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
            if (_popupManager == null)
            {
                Debug.LogError("Cannot show booster guide popup because PopupManager is missing.");
                return;
            }

            if (_boosterBuyCatalog == null)
            {
                Debug.LogError("Cannot show booster guide popup because BoosterBuyCatalog is missing.");
                return;
            }

            if (!_boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
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
            _isBoosterGuideShowing = true;
            popup.SetActions(
                new BoosterGuidePopupViewActions(
                    OnBoosterGuideClosed,
                    OnBoosterGuideClosed));
        }

        public void HideBoosterGuidePopup()
        {
            if (_popupManager == null)
            {
                return;
            }

            _popupManager.Hide<BoosterGuidePopupView>();
            _isBoosterGuideShowing = false;
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
            _gameplayHudView.SetCombo(_currentComboCount, _currentComboRemainingSeconds);
            RefreshBoosterHud();
        }

        private CoinRewardOverlayView GetOrCreateCoinRewardOverlay()
        {
            if (_coinRewardOverlayView != null)
            {
                return _coinRewardOverlayView;
            }

            if (_coinRewardOverlayPrefab == null || _rewardEffectRoot == null)
            {
                Debug.LogError("Coin reward overlay prefab or reward effect root is missing.");
                return null;
            }

            _coinRewardOverlayView = Instantiate(_coinRewardOverlayPrefab, _rewardEffectRoot);
            _coinRewardOverlayView.gameObject.name = _coinRewardOverlayPrefab.gameObject.name;
            return _coinRewardOverlayView;
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
            if (_playerProfileService == null)
            {
                return;
            }

            resourceView.SetPlayerResources(
                _playerProfileService.CoinBalance,
                _playerProfileService.GetHeartStatus());
        }

        private void OnHomeCoinArrived()
        {
            _audioService?.PlaySfx(AudioKeys.SfxCoinReceive);
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

            if (_boosterBuyCatalog == null)
            {
                return;
            }

            IReadOnlyList<BoosterBuyContentEntry> entries =
                _boosterBuyCatalog.Entries;

            if (entries == null)
            {
                return;
            }

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
            return _boosterManager != null &&
                   _boosterManager.HasSeenGuide(boosterType);
        }

        private void MarkBoosterGuideSeen(BoosterType boosterType)
        {
            _boosterManager?.MarkGuideSeen(boosterType);
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

                if (_boosterBuyCatalog != null &&
                    _boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
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

