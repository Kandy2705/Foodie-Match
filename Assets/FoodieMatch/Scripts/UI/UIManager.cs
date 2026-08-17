using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Audio;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.GoldPass;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.Features.Motion;
using FoodieMatch.UI.Advertising;
using FoodieMatch.UI.AddressableAssets;
using FoodieMatch.UI.Booster;
using FoodieMatch.UI.BoosterBuy;
using FoodieMatch.UI.BoosterGuide;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Debugging;
using FoodieMatch.UI.Effects;
using FoodieMatch.UI.Gameplay;
using FoodieMatch.UI.GoldPass;
using FoodieMatch.UI.Heart;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.LeaveGame;
using FoodieMatch.UI.LeaderBoard;
using FoodieMatch.UI.Loading;
using FoodieMatch.UI.MainMenu;
using FoodieMatch.UI.Navigation;
using FoodieMatch.UI.Pause;
using FoodieMatch.UI.Popup;
using FoodieMatch.UI.Reward;
using FoodieMatch.UI.Result;
using FoodieMatch.UI.RetryGame;
using FoodieMatch.UI.Revive;
using FoodieMatch.UI.Setting;
using FoodieMatch.UI.Shop;
using FoodieMatch.UI.Social;
using FoodieMatch.UI.StarterPack;
using UnityEngine;

namespace FoodieMatch.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private const string MainMenuHomeInstanceKey =
            "main-menu/home";
        private const string MainMenuShopInstanceKey =
            "main-menu/shop";
        private const string MainMenuSocialInstanceKey =
            "main-menu/social";
        private const string MainMenuLeaderBoardInstanceKey =
            "main-menu/leaderboard";
        private const string StarterPackProductId =
            "starter_pack";

        [Header("Popup")]
        [SerializeField] private PopupManager _popupManager;
        [SerializeField] private UiGlobalButtonClickSfx _uiGlobalButtonClickSfx;

        [Header("HUD")]
        [SerializeField] private Transform _hudRoot;

        [Header("Loading")]
        [SerializeField] private LoadingScreenView _loadingScreenPrefab;
        [SerializeField] private Transform _loadingRoot;
        [SerializeField] private Texture2D _addressableLoadingTexture;

        [Header("Level Warning")]
        [SerializeField] private WarningLevelView _levelWarningPrefab;

        [Header("Effect")]
        [SerializeField] private CoinRewardOverlayView _coinRewardOverlayPrefab;
        [SerializeField] private SpoonRewardOverlayView _spoonRewardOverlayPrefab;
        [SerializeField] private Transform _effectRoot;
        private GameplayClickParticleController _clickParticleController;

        [Header("Action Feedback")]
        [SerializeField] private ActionFeedbackView _actionFeedbackPrefab;

        [Header("Booster Guide")]
        [SerializeField] private BoosterBuyCatalogSO _boosterBuyCatalog;

        private readonly List<BoosterBuyContentEntry> _pendingBoosterGuides = new();

        private BoosterManager _boosterManager;
        private IGameBoosterConfig _boosterConfig;
        private IGameEconomyConfig _economyConfig;
        private IAdvertisingRuntimeSettings _advertisingRuntimeSettings;
        private PlayerProfileService _playerProfileService;
        private GoldPassService _goldPassService;
        private ILevelCatalogRepository _levelCatalogRepository;
        private IGameShopConfig _shopConfig;
        private IAudioService _audioService;
        private IAddressableUiFactory _addressableUiFactory;
        private ComboFeedbackViewPool _comboFeedbackViewPool;
        private ClickParticlePool _clickParticlePool;
        private GameplayEvents _gameplayEvents;
        private GameplayHudView _gameplayHudView;
        private LoadingScreenView _loadingScreenView;
        private AddressableLoadingOverlayView _addressableLoadingOverlay;
        private WarningLevelView _levelWarningView;
        private CoinRewardOverlayView _coinRewardOverlayView;
        private SpoonRewardOverlayView _spoonRewardOverlayView;
        private readonly List<ActionFeedbackView> _actionFeedbackViews = new();
        private BoosterGuideFlowState _boosterGuideFlowState;
        private AddBoxFlowSource _addBoxFlowSource;
        private LeavePopupSource _leavePopupSource;
        private ReviveFlowContext _reviveFlowContext;
        private int _currentLevelNumber = 1;
        private int _currentServedCount;
        private int _currentTotalCount;
        private int _currentComboCount;
        private float _currentComboRemainingSeconds;
        private BoosterType _currentBoosterBuyType;
        private int _pendingUnlockSlotIndex = -1;
        private Action<int> _pendingUnlockCallback;
        private Action _heartRefillCompleted;
        private int _gameplayHudRequestVersion;
        private bool _isAddressableUiLoading;
        private bool _isTransitionLoadingVisible;

        public event Action PlayGameRequested;

        public event Action LeaveGameRequested;

        public event Action<BoosterType> BoosterCoinPurchaseRequested;

        public event Action<BoosterType> BoosterRewardedAdRequested;

        public event Action AddBoxCoinPaymentRequested;

        public event Action AddBoxRewardedAdRequested;

        public event Action FillHeartCoinPurchaseRequested;

        public event Action FillHeartRewardedAdRequested;

        public Func<BoosterType, bool> BoosterUseHandler { get; set; }

        public Func<bool> RestartGameHandler { get; set; }

        public Func<string, Task<ShopPurchaseResult>> ShopPurchaseHandler { get; set; }

        private void OnDestroy()
        {
            CompleteHomeRewardPresentationImmediately();
            _loadingScreenView?.HideImmediately();

            if (_addressableUiFactory != null)
            {
                _addressableUiFactory.LoadingStateChanged -=
                    OnAddressableUiLoadingStateChanged;
            }

            ReleaseMainMenuViews();
            _popupManager.Shutdown();
            _addressableUiFactory?.ReleaseAll();
            UnsubscribeEvents();
        }

        public void Construct(
            GameplayEvents gameplayEvents,
            IAudioService audioService,
            BoosterManager boosterManager,
            IGameBoosterConfig boosterConfig,
            IGameEconomyConfig economyConfig,
            IAdvertisingRuntimeSettings advertisingRuntimeSettings,
            PlayerProfileService playerProfileService,
            GoldPassService goldPassService,
            ILevelCatalogRepository levelCatalogRepository,
            IGameShopConfig shopConfig,
            IAddressableUiFactory addressableUiFactory,
            ComboFeedbackViewPool comboFeedbackViewPool,
            ClickParticlePool clickParticlePool,
            GameplayPointerInput gameplayPointerInput)
        {
            _addressableUiFactory = addressableUiFactory ??
                throw new ArgumentNullException(nameof(addressableUiFactory));
            _addressableLoadingOverlay =
                AddressableLoadingOverlayView.Create(
                    _loadingRoot,
                    _addressableLoadingTexture);
            _addressableUiFactory.LoadingStateChanged +=
                OnAddressableUiLoadingStateChanged;
            _popupManager.Construct(addressableUiFactory);
            _audioService = audioService;
            _uiGlobalButtonClickSfx.Construct(audioService);

            _gameplayEvents = gameplayEvents;
            _boosterManager = boosterManager;
            _boosterConfig = boosterConfig;
            _economyConfig = economyConfig;
            _advertisingRuntimeSettings = advertisingRuntimeSettings;
            _playerProfileService = playerProfileService;
            _goldPassService = goldPassService;
            _levelCatalogRepository = levelCatalogRepository;
            _shopConfig = shopConfig;
            _comboFeedbackViewPool = comboFeedbackViewPool;
            _clickParticlePool = clickParticlePool;
            _clickParticleController = new GameplayClickParticleController(
                gameplayPointerInput,
                PlayClickParticle);
            RefreshClickParticleState();
            SubscribeEvents();
        }

        public void PlayClickParticle(Vector2 screenPosition)
        {
            _clickParticlePool.Play(
                (RectTransform)_effectRoot,
                screenPosition);
        }

        public void ReleaseClickParticles()
        {
            _clickParticlePool.ReleaseAll();
        }

        private void OnAddressableUiLoadingStateChanged(bool isLoading)
        {
            _isAddressableUiLoading = isLoading;
            RefreshAddressableLoadingOverlay();
            RefreshClickParticleState();
        }

        private void RefreshAddressableLoadingOverlay()
        {
            bool showOverlay =
                _isAddressableUiLoading &&
                !_isTransitionLoadingVisible;

            Transform overlayParent = _loadingRoot;

            if (_popupManager.TryGetOpened(
                    out MainMenuView mainMenuView) &&
                mainMenuView.IsVisible)
            {
                overlayParent = mainMenuView.ViewContainer;
            }

            if (showOverlay &&
                overlayParent == _loadingRoot)
            {
                _loadingRoot.SetAsLastSibling();
            }

            _addressableLoadingOverlay.SetVisible(
                showOverlay,
                overlayParent);
        }

        public void ShowHome(long displayedCoinBalance)
        {
            RunUiTask(
                ShowHomeAsync(displayedCoinBalance),
                nameof(ShowHome));
        }

        public async Task ShowHomeAsync(long displayedCoinBalance)
        {
            CompleteHomeRewardPresentationImmediately();

            await _addressableUiFactory.PreloadLabelAsync(
                UiAddressLabels.BootstrapCritical);

            MainMenuView mainMenuView =
                await _popupManager.ShowAsync<MainMenuView>();

            if (this == null)
            {
                return;
            }

            mainMenuView.SetViewLoader(
                tab => LoadMainMenuViewAsync(mainMenuView, tab));
            mainMenuView.SetTabSelectedAction(OnMainMenuTabSelected);

            HomeView homeView =
                (HomeView)await LoadMainMenuViewAsync(
                    mainMenuView,
                    BottomNavigationTab.Home);
            mainMenuView.RegisterView(
                BottomNavigationTab.Home,
                homeView);
            ConfigureHomeView(homeView, displayedCoinBalance);

            _addressableUiFactory.ReleaseLabel(
                UiAddressLabels.GameplayCritical);
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

        public void PlayHomeSpoonReward(int spoonCount)
        {
            if (!_popupManager.TryGetOpened(out MainMenuView mainMenuView))
            {
                return;
            }

            if (!mainMenuView.TryGetView<HomeView>(out HomeView homeView))
            {
                return;
            }

            SpoonRewardOverlayView spoonRewardOverlay =
                GetOrCreateSpoonRewardOverlay();
            spoonRewardOverlay.PlaySpoonReward(
                spoonCount,
                homeView.GetGoldPassRewardTarget(),
                OnHomeSpoonArrived);
        }

        public void CompleteHomeRewardPresentationImmediately()
        {
            _coinRewardOverlayView?.CompleteRewardImmediately();
            _spoonRewardOverlayView?.StopReward();
        }

        public void SetCurrentLevelNumber(int levelNumber)
        {
            _currentLevelNumber = levelNumber;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.SetLevelNumber(levelNumber);
                _gameplayHudView.SetPauseButtonVisible(levelNumber > 1);
            }
        }

        public void SetGameplayControlsInteractable(bool interactable)
        {
            _gameplayHudView.SetControlsInteractable(interactable);
        }

        public void RefreshHomeLevel()
        {
            if (_popupManager.TryGetOpened(
                    out MainMenuView mainMenuView) &&
                mainMenuView.TryGetView(out HomeView homeView))
            {
                SetHomePlayLevel(homeView);
            }
        }

        public void HideHome()
        {
            CompleteHomeRewardPresentationImmediately();

            _popupManager.Hide<MainMenuView>();
        }

        public async Task PrepareGameplayHudAsync()
        {
            int requestVersion = ++_gameplayHudRequestVersion;

            await _addressableUiFactory.PreloadLabelAsync(
                UiAddressLabels.GameplayCritical);

            if (this == null)
            {
                return;
            }

            if (requestVersion != _gameplayHudRequestVersion)
            {
                return;
            }

            if (_gameplayHudView != null)
            {
                _gameplayHudView.HideInstantly();
                BindGameplayHudActions();
                RefreshClickParticleState();
                _addressableUiFactory.ReleaseLabel(
                    UiAddressLabels.BootstrapCritical);
                return;
            }

            GameplayHudView gameplayHudView =
                await _addressableUiFactory.GetOrCreateAsync<GameplayHudView>(
                    UiAddressKeys.GameplayRoot,
                    _hudRoot);

            if (this == null)
            {
                return;
            }

            _gameplayHudView = gameplayHudView;
            _gameplayHudView.Construct(
                _comboFeedbackViewPool);

            if (requestVersion != _gameplayHudRequestVersion)
            {
                _gameplayHudView.HideInstantly();
                RefreshClickParticleState();
                return;
            }

            _gameplayHudView.HideInstantly();
            BindGameplayHudActions();
            RefreshClickParticleState();
            _addressableUiFactory.ReleaseLabel(
                UiAddressLabels.BootstrapCritical);
        }

        public async Task OpenGameplayHudAsync()
        {
            Task openTask = _gameplayHudView.OpenAsync();
            RefreshClickParticleState();
            await openTask;
        }

        public async Task CloseGameplayHudAsync()
        {
            _gameplayHudRequestVersion++;

            if (_gameplayHudView != null)
            {
                await _gameplayHudView.CloseAsync();
            }

            ReleaseClickParticles();
            RefreshClickParticleState();
        }

        public void HideGameplayHudInstantly()
        {
            _gameplayHudRequestVersion++;

            if (_gameplayHudView != null)
            {
                _gameplayHudView.HideInstantly();
            }

            ReleaseClickParticles();
            RefreshClickParticleState();
        }

        public void ShowComboFeedback(Vector3 worldPosition)
        {
            _gameplayHudView?.ShowComboFeedback(worldPosition);
        }

        public void ShowTutorialHand(Vector2 screenPosition)
        {
            _gameplayHudView.ShowTutorialHand(screenPosition);
        }

        public void ShowTutorial()
        {
            _gameplayHudView.ShowTutorial();
        }

        public Task<MotionResult> MoveTutorialHandAsync(Vector2 screenPosition)
        {
            return _gameplayHudView.MoveTutorialHandAsync(screenPosition);
        }

        public void HideTutorialHand()
        {
            _gameplayHudView?.HideTutorialHand();
        }

        public void HideTutorial()
        {
            _gameplayHudView?.HideTutorial();
        }

        public void ShowActionFeedback(string message)
        {
            ActionFeedbackView actionFeedback = Instantiate(
                _actionFeedbackPrefab,
                _effectRoot);
            actionFeedback.gameObject.name = _actionFeedbackPrefab.gameObject.name;
            _actionFeedbackViews.Add(actionFeedback);
            actionFeedback.Show(message, OnActionFeedbackHidden);
            RefreshActionFeedbackPositions();
        }

        public Task PlayLoadingAsync()
        {
            return BeginLoading().ShowAsync();
        }

        public void ShowLoadingImmediately()
        {
            BeginLoading().ShowImmediately();
        }

        public void SetLoadingProgress(float progress)
        {
            _loadingScreenView.SetProgress(progress);
        }

        public async Task PlayLevelWarningAsync(LevelDifficulty difficulty)
        {
            await HideTransitionLoadingScreenAsync();

            if (difficulty == LevelDifficulty.Normal)
            {
                return;
            }

            if (_levelWarningView == null)
            {
                _levelWarningView = Instantiate(
                    _levelWarningPrefab,
                    _effectRoot);
                _levelWarningView.gameObject.name = _levelWarningPrefab.gameObject.name;
            }

            await _levelWarningView.PlayAsync(difficulty);
        }

        public Task HideTransitionLoadingAsync()
        {
            return HideTransitionLoadingScreenAsync();
        }

        public void HideLoading()
        {
            RunUiTask(
                HideLoadingAsync(),
                nameof(HideLoading));
        }

        public async Task HideLoadingAsync()
        {
            await HideTransitionLoadingScreenAsync();
            TryShowNextBoosterGuide();
        }

        private async Task HideTransitionLoadingScreenAsync()
        {
            if (_loadingScreenView != null)
            {
                await _loadingScreenView.HideAsync();
            }

            if (this == null)
            {
                return;
            }

            _isTransitionLoadingVisible = false;
            RefreshAddressableLoadingOverlay();
            RefreshClickParticleState();
        }

        private void RefreshClickParticleState()
        {
            bool isGameplayHudVisible =
                _gameplayHudView != null &&
                _gameplayHudView.gameObject.activeSelf;
            bool effectEnabled =
                isGameplayHudVisible &&
                !_isTransitionLoadingVisible &&
                !_isAddressableUiLoading;

            _clickParticleController.SetEffectEnabled(effectEnabled);
        }

        public void ShowSettingPopup()
        {
            RunUiTask(
                ShowPopupAsync<SettingPopupView>(
                    data: null,
                    settingPopup =>
                    {
                        settingPopup.SetActions(
                            new SettingPopupViewActions(
                                OnSettingCloseClicked,
                                OnSettingSoundChanged,
                                OnSettingMusicChanged,
                                OnSettingDebugMenuRequested));
                        settingPopup.SetToggleStates(
                            _audioService.IsSfxEnabled,
                            _audioService.IsMusicEnabled);
                    }),
                nameof(ShowSettingPopup));
        }

        public void HideSettingPopup()
        {
            _popupManager.Hide<SettingPopupView>();
        }

        private void ShowPlayerDebugPopup()
        {
            HeartStatus heartStatus =
                _playerProfileService.GetHeartStatus();
            GoldPassStatus goldPassStatus = _goldPassService.GetStatus();

            PlayerProfileDebugUpdate playerProfile = new(
                _playerProfileService.CurrentLevelNumber,
                _playerProfileService.CoinBalance,
                heartStatus.HeartCount,
                _boosterManager.GetCount(BoosterType.Plate),
                _boosterManager.GetCount(BoosterType.Storage),
                _boosterManager.GetCount(BoosterType.Swap),
                _boosterManager.GetCount(BoosterType.Fridge));
            DebugMenuValues values = new(
                playerProfile,
                goldPassStatus.SpoonCount,
                goldPassStatus.IsSeasonPassPurchased,
                _advertisingRuntimeSettings.PostLevelAdsEnabled,
                _advertisingRuntimeSettings.UseLevelPlayAds);

            RunUiTask(
                ShowPopupAsync<PlayerDebugPopupView>(
                    data: null,
                    popup =>
                    {
                        popup.SetActions(
                            new PlayerDebugPopupViewActions(
                                HidePlayerDebugPopup,
                                OnPlayerDebugApplyClicked,
                                OnResetGoldPassClaimHistoryClicked));
                        popup.SetValues(values, heartStatus.MaxHeartCount);
                    }),
                nameof(ShowPlayerDebugPopup));
        }

        private void HidePlayerDebugPopup()
        {
            _popupManager.Hide<PlayerDebugPopupView>();
        }

        public void ShowPausePopup()
        {
            RunUiTask(
                ShowPopupAsync<PauseView>(
                    data: null,
                    pauseView =>
                    {
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
                    }),
                nameof(ShowPausePopup));
        }

        public void HidePausePopup()
        {
            _popupManager.Hide<PauseView>();
        }

        public void ShowLeaveGamePopup()
        {
            RunUiTask(
                ShowPopupAsync<LeaveGamePopupView>(
                    data: null,
                    leaveGamePopup =>
                        leaveGamePopup.SetActions(
                            new LeaveGamePopupViewActions(
                                OnLeaveGameCloseClicked,
                                OnLeaveGameLeaveClicked))),
                nameof(ShowLeaveGamePopup));
        }

        public void HideLeaveGamePopup()
        {
            _popupManager.Hide<LeaveGamePopupView>();
        }

        public void ShowRetryGamePopup()
        {
            RunUiTask(
                ShowPopupAsync<RetryGamePopupView>(
                    data: null,
                    retryGamePopup =>
                        retryGamePopup.SetActions(
                            new RetryGamePopupViewActions(
                                OnRetryGameCloseClicked,
                                OnRetryGameRetryClicked))),
                nameof(ShowRetryGamePopup));
        }

        public void HideRetryGamePopup()
        {
            _popupManager.Hide<RetryGamePopupView>();
        }

        public void ShowRevivePopup(
            Action loseTryAgainClicked,
            Action loseHomeClicked,
            Action onBoxRescueConfirmed,
            Action onLoseConfirmed)
        {
            _leavePopupSource = LeavePopupSource.None;
            _reviveFlowContext = new(
                loseTryAgainClicked,
                loseHomeClicked,
                onBoxRescueConfirmed,
                onLoseConfirmed);

            OpenRevivePopup();
        }

        private void OpenRevivePopup()
        {
            _addBoxFlowSource = AddBoxFlowSource.Revive;
            RunUiTask(
                ShowPopupAsync<RevivePopupView>(
                    data: null,
                    revivePopup =>
                    {
                        revivePopup.SetActions(
                            new RevivePopupViewActions(
                                OnReviveCloseClicked,
                                OnReviveFreeAdsClicked,
                                OnRevivePlayOnClicked));
                        revivePopup.SetCost(
                            _economyConfig
                                .GetBoosterPrice(BoosterType.Box)
                                .ToString());
                        SetCurrentPlayerResources(revivePopup);
                    }),
                nameof(OpenRevivePopup));
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
            RunUiTask(
                ShowResultPopupAsync<WinView>(
                    data: null,
                    winView =>
                    {
                        _audioService.PlaySfx(AudioKeys.SfxWinGame);
                        winView.SetActions(
                            new WinViewActions(
                                claimCoinRewardClicked,
                                doubleCoinRewardClicked));
                        winView.SetRewardAmounts(
                            regularRewardAmount,
                            doubleRewardAmount);
                    }),
                nameof(ShowWinPopup));
        }

        public void HideWinPopup()
        {
            _popupManager.Hide<WinView>();
        }

        public bool ShowFakeRewardedAdPopup(
            Action completed,
            Action cancelled)
        {
            RunUiTask(
                ShowPopupAsync<FakeRewardedAdPopupView>(
                    data: null,
                    popup =>
                        popup.SetActions(
                            new FakeRewardedAdPopupViewActions(
                                completed,
                                cancelled))),
                nameof(ShowFakeRewardedAdPopup),
                cancelled);
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
            RunUiTask(
                ShowResultPopupAsync<LoseView>(
                    data: null,
                    loseView =>
                    {
                        _audioService.PlaySfx(AudioKeys.SfxLoseGame);
                        loseView.SetActions(
                            new LoseViewActions(
                                tryAgainClicked,
                                homeClicked));
                        SetCurrentPlayerResources(loseView);
                    }),
                nameof(ShowLosePopup));
        }

        public void HideLosePopup()
        {
            _popupManager.Hide<LoseView>();
        }

        public void ShowFillHeartPopup(Action heartRefillCompleted = null)
        {
            HeartStatus heartStatus = _playerProfileService.GetHeartStatus();

            if (heartStatus.IsFull || heartStatus.IsUnlimited)
            {
                return;
            }

            _heartRefillCompleted = heartRefillCompleted;

            RunUiTask(
                ShowPopupAsync<FillHeartPopupView>(
                    data: null,
                    popup =>
                    {
                        popup.SetActions(
                            new FillHeartPopupViewActions(
                                OnFillHeartCloseClicked,
                                OnFillHeartFreeAdsClicked,
                                OnFillHeartBuyClicked,
                                OnHeartRecoveredToFull));
                        popup.SetFullHeartCoinPrice(
                            _economyConfig.FullHeartCoinPrice);
                        popup.SetPlayerResources(
                            _playerProfileService.CoinBalance,
                            heartStatus);
                        popup.SetResourceClickActions(
                            ShowShopPopup,
                            ShowShopPopup);
                    }),
                nameof(ShowFillHeartPopup));
        }

        public void CompleteHeartRefill()
        {
            Action heartRefillCompleted = _heartRefillCompleted;
            _heartRefillCompleted = null;
            _popupManager.Hide<FillHeartPopupView>();
            RefreshAllPlayerResources();
            heartRefillCompleted?.Invoke();
        }

        public BoosterSwapPopup ShowSwapPopup()
        {
            if (_popupManager.TryGetOpened(out BoosterSwapPopup popup))
            {
                return popup;
            }

            throw new InvalidOperationException(
                "BoosterSwapPopup is loaded asynchronously. " +
                "Use ShowSwapPopupAsync for the first show.");
        }

        public Task<BoosterSwapPopup> ShowSwapPopupAsync()
        {
            return _popupManager.ShowAsync<BoosterSwapPopup>();
        }

        public void HideSwapPopup()
        {
            _popupManager.Hide<BoosterSwapPopup>();
        }

        public void HideAllPopups()
        {
            CompleteHomeRewardPresentationImmediately();
            _pendingBoosterGuides.Clear();
            _boosterGuideFlowState = BoosterGuideFlowState.Idle;
            _gameplayHudView?.StopBoosterUnlockReward();
            _addBoxFlowSource = AddBoxFlowSource.None;
            _leavePopupSource = LeavePopupSource.None;
            _reviveFlowContext = null;
            _heartRefillCompleted = null;
            ClearLockedPackageUnlock();

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

            _currentBoosterBuyType = boosterType;
            RunUiTask(
                ShowPopupAsync<BoosterBuyPopupView>(
                    popupData,
                    popup =>
                    {
                        popup.SetActions(
                            new BoosterBuyPopupViewActions(
                                OnBoosterBuyCloseClicked,
                                OnBoosterBuyFreeAdsClicked,
                                OnBoosterBuyBuyClicked));
                        SetCurrentPlayerResources(popup);
                    }),
                nameof(ShowBoosterBuyPopup));
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
            RefreshAllPlayerResources();
        }

        public void RefreshAllPlayerResources()
        {
            long coinBalance = _playerProfileService.CoinBalance;

            HeartStatus heartStatus =
                _playerProfileService.GetHeartStatus();

            if (_popupManager.TryGetOpened(out MainMenuView mainMenuView) &&
                mainMenuView.TryGetView<HomeView>(out HomeView homeView))
            {
                homeView.SetPlayerResources(coinBalance, heartStatus);
            }

            if (_popupManager.TryGetOpened(out mainMenuView) &&
                mainMenuView.TryGetView<ShopView>(out ShopView shopView))
            {
                shopView.SetPlayerResources(coinBalance, heartStatus);
            }

            if (_popupManager.TryGetOpened(out ShopView popupShopView))
            {
                popupShopView.SetPlayerResources(coinBalance, heartStatus);
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

            if (_popupManager.TryGetOpened(out FillHeartPopupView fillHeartPopup))
            {
                fillHeartPopup.SetPlayerResources(coinBalance, heartStatus);
            }

            RefreshBoosterHud();
        }

        public void ShowUnlockLockedPackagePopup(int slotIndex, Action<int> onUnlockConfirmed)
        {
            int coinPrice = _economyConfig.GetBoosterPrice(BoosterType.Box);
            _addBoxFlowSource = AddBoxFlowSource.LockedPackage;
            _pendingUnlockSlotIndex = slotIndex;
            _pendingUnlockCallback = onUnlockConfirmed;

            RunUiTask(
                ShowPopupAsync<RevivePopupView>(
                    data: null,
                    popup =>
                    {
                        popup.SetActions(
                            new RevivePopupViewActions(
                                OnUnlockPopupCloseClicked,
                                OnUnlockPopupFreeAdsClicked,
                                OnUnlockPopupBuyClicked));
                        popup.SetCost(coinPrice.ToString());
                        SetCurrentPlayerResources(popup);
                    }),
                nameof(ShowUnlockLockedPackagePopup));
        }

        private void OnUnlockPopupCloseClicked()
        {
            _addBoxFlowSource = AddBoxFlowSource.None;
            ClearLockedPackageUnlock();
            HideRevivePopup();
        }

        private void OnUnlockPopupFreeAdsClicked()
        {
            AddBoxRewardedAdRequested();
        }

        private void OnUnlockPopupBuyClicked()
        {
            AddBoxCoinPaymentRequested();
        }

        public void CompleteAddBoxRequest()
        {
            AddBoxFlowSource source = _addBoxFlowSource;
            _addBoxFlowSource = AddBoxFlowSource.None;

            switch (source)
            {
                case AddBoxFlowSource.LockedPackage:
                    CompleteLockedPackageUnlock();
                    return;
                case AddBoxFlowSource.Revive:
                    ConfirmBoxRescue();
                    return;
                default:
                    throw new InvalidOperationException(
                        "No Add Box request is waiting for completion.");
            }
        }

        private void CompleteLockedPackageUnlock()
        {
            int slotIndex = _pendingUnlockSlotIndex;
            Action<int> callback = _pendingUnlockCallback;
            ClearLockedPackageUnlock();
            HideRevivePopup();
            callback(slotIndex);
        }

        private void ClearLockedPackageUnlock()
        {
            _pendingUnlockSlotIndex = -1;
            _pendingUnlockCallback = null;
        }

        public void ShowBoosterGuidePopup(BoosterType boosterType)
        {
            if (!_boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
            {
                Debug.LogError($"Booster guide content not found for type: {boosterType}");
                return;
            }

            BoosterGuidePopupData popupData = BoosterGuidePopupData.FromCatalogEntry(entry);
            _boosterGuideFlowState = BoosterGuideFlowState.ShowingGuide;
            RunUiTask(
                ShowPopupAsync<BoosterGuidePopupView>(
                    popupData,
                    popup =>
                        popup.SetActions(
                            new BoosterGuidePopupViewActions(
                                () => OnBoosterGuideConfirmed(
                                    boosterType,
                                    entry.Icon)))),
                nameof(ShowBoosterGuidePopup),
                OnBoosterGuideFlowFailed);
        }

        public void HideBoosterGuidePopup()
        {
            _popupManager.Hide<BoosterGuidePopupView>();
        }

        private void BindGameplayHudActions()
        {
            _gameplayHudView.SetActions(
                new GameplayHudViewActions(
                    OnGameplayPauseRequested,
                    OnGameplayBoosterUseRequested,
                    OnGameplayBoosterAddRequested));
            _gameplayHudView.SetLevelNumber(_currentLevelNumber);
            _gameplayHudView.SetPauseButtonVisible(
                _currentLevelNumber > 1);
            _gameplayHudView.SetProgress(_currentServedCount, _currentTotalCount);
            _gameplayHudView.SetCombo(_currentComboCount, _currentComboRemainingSeconds);
            RefreshBoosterHud();
        }

        private void BindShopView(ShopView shopView)
        {
            shopView.SetPurchaseHandler(ShopPurchaseHandler);
            shopView.SetResourceRefreshHandler(
                () => shopView.SetPlayerResources(
                    _playerProfileService.CoinBalance,
                    _playerProfileService.GetHeartStatus()));
            shopView.Bind(_shopConfig);
            shopView.SetPlayerResources(
                _playerProfileService.CoinBalance,
                _playerProfileService.GetHeartStatus());
            shopView.SetResourceClickActions(null, null);
        }

        private async Task<MonoBehaviour> LoadMainMenuViewAsync(
            MainMenuView mainMenuView,
            BottomNavigationTab tab)
        {
            switch (tab)
            {
                case BottomNavigationTab.Home:
                    return await _addressableUiFactory
                        .GetOrCreateAsync<HomeView>(
                            UiAddressKeys.HomeScreen,
                            MainMenuHomeInstanceKey,
                            mainMenuView.ViewContainer);

                case BottomNavigationTab.Shop:
                    ShopView shopView = await _addressableUiFactory
                        .GetOrCreateAsync<ShopView>(
                            UiAddressKeys.ShopScreen,
                            MainMenuShopInstanceKey,
                            mainMenuView.ViewContainer);
                    BindShopView(shopView);
                    return shopView;

                case BottomNavigationTab.Social:
                    return await _addressableUiFactory
                        .GetOrCreateAsync<SocialView>(
                            UiAddressKeys.SocialScreen,
                            MainMenuSocialInstanceKey,
                            mainMenuView.ViewContainer);

                case BottomNavigationTab.LeaderBoard:
                    return await _addressableUiFactory
                        .GetOrCreateAsync<LeaderBoardView>(
                            UiAddressKeys.LeaderBoardScreen,
                            MainMenuLeaderBoardInstanceKey,
                            mainMenuView.ViewContainer);

                default:
                    throw new InvalidOperationException(
                        $"Main menu tab {tab} does not have an Addressable view.");
            }
        }

        private void ConfigureHomeView(
            HomeView homeView,
            long displayedCoinBalance)
        {
            homeView.SetActions(
                new HomeViewActions(
                    OnHomePlayRequested,
                    OnHomeSettingRequested,
                    OnHomeStarterPackRequested,
                    OnHomeGoldPassRequested,
                    OnHomeCoinClicked,
                    OnHomeHeartClicked));
            SetHomePlayLevel(homeView);
            homeView.SetPlayerResources(
                displayedCoinBalance,
                _playerProfileService.GetHeartStatus());
        }

        private void ReleaseMainMenuViews()
        {
            if (_addressableUiFactory == null)
            {
                return;
            }

            _addressableUiFactory.Release(MainMenuHomeInstanceKey);
            _addressableUiFactory.Release(MainMenuShopInstanceKey);
            _addressableUiFactory.Release(MainMenuSocialInstanceKey);
            _addressableUiFactory.Release(MainMenuLeaderBoardInstanceKey);
        }

        private CoinRewardOverlayView GetOrCreateCoinRewardOverlay()
        {
            if (_coinRewardOverlayView != null)
            {
                return _coinRewardOverlayView;
            }

            _coinRewardOverlayView = Instantiate(_coinRewardOverlayPrefab, _effectRoot);
            _coinRewardOverlayView.gameObject.name = _coinRewardOverlayPrefab.gameObject.name;
            return _coinRewardOverlayView;
        }

        private SpoonRewardOverlayView GetOrCreateSpoonRewardOverlay()
        {
            if (_spoonRewardOverlayView != null)
            {
                return _spoonRewardOverlayView;
            }

            _spoonRewardOverlayView = Instantiate(
                _spoonRewardOverlayPrefab,
                _effectRoot);
            _spoonRewardOverlayView.gameObject.name =
                _spoonRewardOverlayPrefab.gameObject.name;
            return _spoonRewardOverlayView;
        }

        private void OnActionFeedbackHidden(ActionFeedbackView actionFeedback)
        {
            _actionFeedbackViews.Remove(actionFeedback);
            RefreshActionFeedbackPositions();
        }

        private void RefreshActionFeedbackPositions()
        {
            for (int index = 0; index < _actionFeedbackViews.Count; index++)
            {
                _actionFeedbackViews[index].SetStackIndex(index);
            }
        }

        private LoadingScreenView GetOrCreateLoadingScreen()
        {
            if (_loadingScreenView != null)
            {
                return _loadingScreenView;
            }

            _loadingRoot.SetAsLastSibling();
            _loadingScreenView = Instantiate(
                _loadingScreenPrefab,
                _loadingRoot);
            _loadingScreenView.gameObject.name =
                _loadingScreenPrefab.gameObject.name;
            return _loadingScreenView;
        }

        private LoadingScreenView BeginLoading()
        {
            _isTransitionLoadingVisible = true;
            RefreshAddressableLoadingOverlay();
            RefreshClickParticleState();
            return GetOrCreateLoadingScreen();
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
            CompleteHomeRewardPresentationImmediately();
            PlayGameRequested?.Invoke();
        }

        private void OnMainMenuTabSelected(BottomNavigationTab tab)
        {
            if (tab != BottomNavigationTab.Home)
            {
                _spoonRewardOverlayView?.StopReward();
            }
        }

        private void OnHomeSettingRequested()
        {
            ShowSettingPopup();
        }

        private void OnHomeStarterPackRequested()
        {
            RunUiTask(
                ShowPopupAsync<StarterPackPopupView>(
                    null,
                    popup => popup.SetActions(
                        new StarterPackPopupViewActions(
                            OnStarterPackBuyRequestedAsync))),
                nameof(OnHomeStarterPackRequested));
        }

        private void OnHomeGoldPassRequested()
        {
            if (_currentLevelNumber < 15)
            {
                return;
            }

            RunUiTask(
                ShowGoldPassAsync(),
                nameof(OnHomeGoldPassRequested));
        }

        private async Task ShowGoldPassAsync()
        {
            GoldPassView goldPassView =
                await _popupManager.ShowAsync<GoldPassView>();

            if (this == null)
            {
                return;
            }

            BindGoldPassView(goldPassView);
        }

        private void BindGoldPassView(GoldPassView goldPassView)
        {
            goldPassView.SetActions(
                new GoldPassViewActions(
                    OnGoldPassCloseClicked,
                    OnGoldPassInformationClicked,
                    OnGoldPassPurchaseClicked,
                    OnGoldPassClaimClicked,
                    OnGoldPassSeasonExpired));
            goldPassView.Bind(_goldPassService.GetStatus());
            goldPassView.ScrollToCurrentMilestone();
        }

        private void OnGoldPassCloseClicked()
        {
            _popupManager.Hide<GoldPassView>();
        }

        private static void OnGoldPassInformationClicked()
        {
            Debug.Log("Gold Pass information is not available yet.");
        }

        private static void OnGoldPassPurchaseClicked()
        {
            Debug.Log("Gold Pass purchase is not available yet.");
        }

        private void OnGoldPassClaimClicked(
            int milestoneLevel,
            GoldPassTrack track)
        {
            GoldPassClaimResult result =
                _goldPassService.TryClaim(milestoneLevel, track);

            if (result == GoldPassClaimResult.Succeeded)
            {
                RefreshAllPlayerResources();
            }

            RefreshOpenedGoldPass();
        }

        private void OnGoldPassSeasonExpired()
        {
            RefreshOpenedGoldPass();
        }

        private void RefreshOpenedGoldPass()
        {
            if (_popupManager.TryGetOpened(out GoldPassView goldPassView))
            {
                goldPassView.Bind(_goldPassService.GetStatus());
            }
        }

        private Task<ShopPurchaseResult> OnStarterPackBuyRequestedAsync()
        {
            return ShopPurchaseHandler(StarterPackProductId);
        }

        private void OnHomeCoinClicked()
        {
            if (_popupManager.TryGetOpened(out MainMenuView mainMenuView))
            {
                mainMenuView.SelectTab(BottomNavigationTab.Shop);
            }
        }

        private void OnHomeHeartClicked()
        {
            ShowFillHeartPopup();
        }

        public void ShowShopPopup()
        {
            RunUiTask(
                ShowShopPopupAsync(),
                nameof(ShowShopPopup));
        }

        private async Task ShowShopPopupAsync()
        {
            ShopView shopView =
                await _popupManager.ShowAsync<ShopView>(
                    ShopPopupData.Instance);

            if (this == null)
            {
                return;
            }

            BindShopView(shopView);
        }

        private void OnFillHeartCloseClicked()
        {
            _heartRefillCompleted = null;
            _popupManager.Hide<FillHeartPopupView>();
        }

        private void OnFillHeartFreeAdsClicked()
        {
            FillHeartRewardedAdRequested?.Invoke();
        }

        private void OnFillHeartBuyClicked()
        {
            FillHeartCoinPurchaseRequested?.Invoke();
        }

        private void OnHeartRecoveredToFull()
        {
            HeartStatus heartStatus =
                _playerProfileService.GetHeartStatus();

            if (heartStatus.IsFull || heartStatus.IsUnlimited)
            {
                CompleteHeartRefill();
            }
        }

        private void SetCurrentPlayerResources(
            IPlayerResourceView resourceView)
        {
            resourceView.SetPlayerResources(
                _playerProfileService.CoinBalance,
                _playerProfileService.GetHeartStatus());
            resourceView.SetResourceClickActions(
                ShowShopPopup,
                ShowShopPopup);
        }

        private void OnHomeCoinArrived()
        {
            _audioService.PlaySfx(AudioKeys.SfxCoinReceive);
        }

        private void OnHomeSpoonArrived()
        {
            _audioService.PlaySfx(AudioKeys.SfxClaim);
        }

        private void OnGameplayPauseRequested()
        {
            ShowPausePopup();
        }

        private void OnGameplayBoosterUseRequested(int boosterIndex)
        {
            if (!BoosterBuyCatalogSO.TryFromButtonIndex(boosterIndex, out BoosterType boosterType))
            {
                return;
            }

            if (!IsBoosterUnlocked(boosterType))
            {
                ShowActionFeedback("This booster is locked.");
                return;
            }

            if (!_boosterManager.HasCount(boosterType))
            {
                ShowActionFeedback("You don't have this booster.");
                return;
            }

            if (BoosterUseHandler == null || !BoosterUseHandler.Invoke(boosterType))
            {
                ShowActionFeedback("This booster can't be used right now.");
                return;
            }
            RefreshBoosterHud();
        }

        private void OnGameplayBoosterAddRequested(int boosterIndex)
        {
            if (!BoosterBuyCatalogSO.TryFromButtonIndex(boosterIndex, out BoosterType boosterType))
            {
                return;
            }

            if (!IsBoosterUnlocked(boosterType))
            {
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

        private void OnSettingDebugMenuRequested()
        {
            ShowPlayerDebugPopup();
        }

        private void OnPlayerDebugApplyClicked(
            DebugMenuValues values)
        {
            PlayerProfileDebugUpdate playerProfile = values.PlayerProfile;

            if (!_levelCatalogRepository.TryGetLevelSummary(
                    playerProfile.CurrentLevelNumber,
                    out _))
            {
                ShowPlayerDebugStatus(
                    $"Level {playerProfile.CurrentLevelNumber} does not exist.");
                return;
            }

            bool adServiceChanged =
                _advertisingRuntimeSettings.UseLevelPlayAds !=
                values.UseLevelPlayAds;
            CompleteHomeRewardPresentationImmediately();
            _playerProfileService.ApplyDebugUpdate(playerProfile);
            _goldPassService.ApplyDebugUpdate(
                values.GoldPassSpoonCount,
                values.IsSeasonPassPurchased);
            _advertisingRuntimeSettings.Update(
                values.PostLevelAdsEnabled,
                values.UseLevelPlayAds);
            SetCurrentLevelNumber(playerProfile.CurrentLevelNumber);
            RefreshHomePlayerData();
            ShowPlayerDebugStatus(
                adServiceChanged
                    ? "Applied. Restart to change ad service."
                    : "Applied");
        }

        private void OnResetGoldPassClaimHistoryClicked()
        {
            _goldPassService.ResetClaimHistory();
            ShowPlayerDebugStatus("Gold Pass claim history reset.");
        }

        private void RefreshHomePlayerData()
        {
            if (!_popupManager.TryGetOpened(
                    out MainMenuView mainMenuView) ||
                !mainMenuView.TryGetView(
                    out HomeView homeView))
            {
                return;
            }

            SetHomePlayLevel(homeView);
            homeView.SetPlayerResources(
                _playerProfileService.CoinBalance,
                _playerProfileService.GetHeartStatus());
        }

        private void ShowPlayerDebugStatus(string message)
        {
            if (_popupManager.TryGetOpened(
                    out PlayerDebugPopupView popup))
            {
                popup.ShowStatus(message);
            }
        }

        private void SetHomePlayLevel(HomeView homeView)
        {
            if (!_levelCatalogRepository.TryGetLevelSummary(
                    _currentLevelNumber,
                    out LevelSummary level))
            {
                Debug.LogError($"Level {_currentLevelNumber} could not be loaded.");
                return;
            }

            homeView.SetPlayLevel(level.LevelNumber, level.Difficulty);
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
            _leavePopupSource = LeavePopupSource.Pause;
            ShowLeaveGamePopup();
        }

        private void OnLeaveGameCloseClicked()
        {
            HideLeaveGamePopup();
            LeavePopupSource source = _leavePopupSource;
            _leavePopupSource = LeavePopupSource.None;

            if (source == LeavePopupSource.Revive)
            {
                OpenRevivePopup();
                return;
            }

            ShowPausePopup();
        }

        private void OnLeaveGameLeaveClicked()
        {
            LeavePopupSource source = _leavePopupSource;
            _leavePopupSource = LeavePopupSource.None;

            if (source == LeavePopupSource.Revive)
            {
                HideLeaveGamePopup();
                ReviveFlowContext context = _reviveFlowContext;
                _reviveFlowContext = null;
                context.LoseConfirmed();
                ShowLosePopup(
                    context.LoseTryAgainClicked,
                    context.LoseHomeClicked);
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
            _addBoxFlowSource = AddBoxFlowSource.None;
            HideRevivePopup();
            _leavePopupSource = LeavePopupSource.Revive;
            ShowLeaveGamePopup();
        }

        private void OnReviveFreeAdsClicked()
        {
            AddBoxRewardedAdRequested();
        }

        private void OnRevivePlayOnClicked()
        {
            AddBoxCoinPaymentRequested();
        }

        private void ConfirmBoxRescue()
        {
            ReviveFlowContext context = _reviveFlowContext;
            _reviveFlowContext = null;
            _leavePopupSource = LeavePopupSource.None;

            HideRevivePopup();
            context.BoxRescueConfirmed();
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
                    GetBoosterUnlockLevel(entry.BoosterType) > levelNumber ||
                    HasSeenBoosterGuide(entry.BoosterType))
                {
                    continue;
                }

                _pendingBoosterGuides.Add(entry);
            }
        }

        private void TryShowNextBoosterGuide()
        {
            if (_boosterGuideFlowState != BoosterGuideFlowState.Idle ||
                _pendingBoosterGuides.Count == 0)
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

        private void OnBoosterGuideConfirmed(
            BoosterType boosterType,
            Sprite icon)
        {
            HideBoosterGuidePopup();

            if (!BoosterBuyCatalogSO.TryGetButtonIndex(
                    boosterType,
                    out int boosterIndex))
            {
                _boosterManager.TryMarkGuideSeen(boosterType);
                CompleteBoosterGuideFlow();
                return;
            }

            _boosterGuideFlowState = BoosterGuideFlowState.PlayingReward;
            RunUiTask(
                PlayBoosterUnlockRewardAsync(
                    boosterType,
                    boosterIndex,
                    icon),
                nameof(PlayBoosterUnlockRewardAsync),
                OnBoosterGuideFlowFailed);
        }

        private async Task PlayBoosterUnlockRewardAsync(
            BoosterType boosterType,
            int boosterIndex,
            Sprite icon)
        {
            MotionResult result =
                await _gameplayHudView.PlayBoosterUnlockRewardAsync(
                    boosterIndex,
                    icon,
                    _boosterConfig.UnlockRewardAmount);

            if (result == MotionResult.Completed &&
                _boosterManager.TryClaimUnlockReward(boosterType))
            {
                RefreshBoosterHud();
            }

            CompleteBoosterGuideFlow();
        }

        private void CompleteBoosterGuideFlow()
        {
            _boosterGuideFlowState = BoosterGuideFlowState.Idle;
            TryShowNextBoosterGuide();
        }

        private void OnBoosterGuideFlowFailed()
        {
            CompleteBoosterGuideFlow();
        }

        private bool HasSeenBoosterGuide(BoosterType boosterType)
        {
            return _boosterManager.HasSeenGuide(boosterType);
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
                unlockedStates[i] = _currentLevelNumber >= unlockLevel &&
                    HasSeenBoosterGuide(boosterType);

                if (_boosterBuyCatalog.TryGet(boosterType, out BoosterBuyContentEntry entry))
                {
                    _gameplayHudView.SetBoosterIconSprites(
                        i,
                        entry.Icon,
                        entry.LockedIconSprite);
                }

                _gameplayHudView.SetBoosterUnlockLevel(i, unlockLevel);
            }

            _gameplayHudView.SetBoosterUnlockedStates(unlockedStates);
        }

        private bool IsBoosterUnlocked(BoosterType boosterType)
        {
            bool hasReachedUnlockLevel =
                _currentLevelNumber >= GetBoosterUnlockLevel(boosterType);

            return hasReachedUnlockLevel &&
                (boosterType == BoosterType.Box ||
                 HasSeenBoosterGuide(boosterType));
        }

        private int GetBoosterUnlockLevel(BoosterType boosterType)
        {
            return _boosterConfig.GetUnlockLevel(boosterType);
        }

        private async Task ShowPopupAsync<TPopup>(
            IPopupData data,
            Action<TPopup> configure)
            where TPopup : PopupBase
        {
            TPopup popup =
                await _popupManager.ShowAsync<TPopup>(data);

            if (this == null)
            {
                return;
            }

            configure(popup);
        }

        private Task ShowResultPopupAsync<TPopup>(
            IPopupData data,
            Action<TPopup> configure)
            where TPopup : PopupBase
        {
            return ShowPopupAsync(data, configure);
        }

        private void RunUiTask(
            Task task,
            string operationName,
            Action failed = null)
        {
            _ = ObserveUiTaskAsync(task, operationName, failed);
        }

        private static async Task ObserveUiTaskAsync(
            Task task,
            string operationName,
            Action failed)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"UI operation {operationName} failed: {exception}");
                failed?.Invoke();
            }
        }

        private enum LeavePopupSource
        {
            None,
            Pause,
            Revive
        }

        private enum AddBoxFlowSource
        {
            None,
            LockedPackage,
            Revive
        }

        private enum BoosterGuideFlowState
        {
            Idle,
            ShowingGuide,
            PlayingReward
        }

        private sealed class ReviveFlowContext
        {
            public ReviveFlowContext(
                Action loseTryAgainClicked,
                Action loseHomeClicked,
                Action boxRescueConfirmed,
                Action loseConfirmed)
            {
                LoseTryAgainClicked = loseTryAgainClicked;
                LoseHomeClicked = loseHomeClicked;
                BoxRescueConfirmed = boxRescueConfirmed;
                LoseConfirmed = loseConfirmed;
            }

            public Action LoseTryAgainClicked { get; }

            public Action LoseHomeClicked { get; }

            public Action BoxRescueConfirmed { get; }

            public Action LoseConfirmed { get; }
        }
    }
}
