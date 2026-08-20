using System.IO;
using FoodieMatch.App.Advertising;
using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Audio;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Configuration;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.GoldPass;
using FoodieMatch.Core.Application.Level;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Purchasing;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.Core.Application.Time;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.Infrastructure.Advertising;
using FoodieMatch.Infrastructure.Audio;
using FoodieMatch.Infrastructure.GoldPass;
using FoodieMatch.Infrastructure.Level;
using FoodieMatch.Infrastructure.Level.Json;
using FoodieMatch.Infrastructure.Level.Remote;
using FoodieMatch.Infrastructure.Persistence.PlayerProfiles;
using FoodieMatch.Infrastructure.Persistence.Configuration;
using FoodieMatch.Infrastructure.Persistence.Save;
using FoodieMatch.Infrastructure.RemoteConfig;
using FoodieMatch.Infrastructure.Purchasing;
using FoodieMatch.Infrastructure.Shop;
using FoodieMatch.Infrastructure.Time;
using FoodieMatch.UI.Advertising;
using FoodieMatch.UI.AddressableAssets;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FoodieMatch.App
{
    public sealed class AppInstaller : MonoBehaviour
    {
        [Header("Advertising")]
        [SerializeField] private string _rewardedAdUnitId;
        [SerializeField] private string _interstitialAdUnitId;

        [Header("Remote Levels")]
        [SerializeField] private string _fallbackLevelManifestUrl;

        public GameplayEvents GameplayEvents { get; private set; }

        public PlayerProfileInitializer PlayerProfileInitializer { get; private set; }

        public FirebaseGameConfigurationLoader GameConfigurationLoader { get; private set; }

        public ILevelSynchronizer LevelSynchronizer { get; private set; }

        public bool Install(AppRoot appRoot)
        {
            if (!TryLoadBundledLevelData(
                    out ResourcesLevelCatalogData bundledLevelData,
                    out LevelContentValidator levelContentValidator))
            {
                return false;
            }

            if (!TryCreateShopConfig(out IGameShopConfig shopConfig))
            {
                return false;
            }

            if (!TryCreateGoldPassConfig(
                    out IGameGoldPassConfig goldPassConfig))
            {
                return false;
            }

            GameplayEvents = new GameplayEvents();

            ISaveService saveService = new PlayerPrefsSaveServiceAdapter();
            GameConfigurationSnapshotSet localConfigDefaults =
                GameConfigurationSnapshotSet.CreateDefaults();
            PlayerPrefsGameConfigurationCache configurationCache = new(saveService);
            GameConfigurationSnapshotSet initialConfig = localConfigDefaults;

            if (configurationCache.TryLoad(out GameConfigurationSnapshotSet cachedConfig))
            {
                initialConfig = cachedConfig;
            }

            GameConfigurationSession configurationSession = new(initialConfig);
            GameConfigurationLoader = new FirebaseGameConfigurationLoader(
                configurationSession,
                localConfigDefaults,
                configurationCache);
            LevelCatalogRepository levelCatalogRepository =
                new(bundledLevelData.Catalog);
            ResourcesLevelRepository resourcesLevelRepository = new(
                new LevelCatalogRepository(bundledLevelData.Catalog),
                bundledLevelData.ContentFiles,
                new LevelContentJsonParser(),
                levelContentValidator,
                new LevelContentMapper());
            LevelDiskCache levelDiskCache = new(
                Path.Combine(
                    Application.persistentDataPath,
                    "LevelCache"));
            levelDiskCache.ClearStaging();
            RemoteLevelManifestCache levelManifestCache =
                new(levelDiskCache);
            RemoteLevelManifestLoader levelManifestLoader = new(
                levelManifestCache,
                _fallbackLevelManifestUrl);
            RemoteLevelPackCache levelPackCache = new(
                levelDiskCache,
                new LevelContentJsonParser(),
                levelContentValidator);
            RemoteLevelPackDownloader levelPackDownloader =
                new(
                    levelPackCache,
                    new RemoteLevelPackArchiveReader());
            LevelSynchronizer = new RemoteLevelSynchronizer(
                bundledLevelData,
                levelCatalogRepository,
                levelManifestLoader,
                levelPackCache,
                levelPackDownloader);
            ILevelRepository levelRepository =
                new RemoteFirstLevelRepository(
                    resourcesLevelRepository,
                    levelManifestLoader,
                    levelPackCache,
                    new LevelContentJsonParser(),
                    levelContentValidator,
                    new LevelContentMapper());
            IAdvertisingRuntimeSettings advertisingRuntimeSettings =
                new PlayerPrefsAdvertisingRuntimeSettings(saveService);
            IGameHeartConfig heartConfig = configurationSession;
            IClock clock = new SystemClock();
            PlayerProfileSession profileSession = new();
            IPlayerProfileRepository profileRepository =
                new PlayerPrefsPlayerProfileRepository(saveService);
            IInvalidPlayerProfileRecovery invalidProfileRecovery =
                new PlayerPrefsInvalidPlayerProfileRecovery(saveService);
            PlayerProfileInitializer = new PlayerProfileInitializer(
                profileRepository,
                invalidProfileRecovery,
                profileSession,
                heartConfig,
                clock);
            PlayerProfileService playerProfileService = new(
                profileRepository,
                profileSession,
                heartConfig,
                clock);
            GoldPassService goldPassService = new(
                goldPassConfig,
                playerProfileService,
                clock);
            IGameGoldPassProgressionConfig goldPassProgressionConfig =
                configurationSession;
            playerProfileService.SaveFailed += LogPlayerProfileSaveFailure;
            IAudioService audioService = CreateAudioService(appRoot, saveService);
            GameplayAudioPresenter gameplayAudioPresenter = new(audioService);
            GameplayPointerInput gameplayPointerInput =
                appRoot.GameplayPointerInput;
            gameplayPointerInput.Construct(EventSystem.current);
            GameplayWorldClickSfx gameplayWorldClickSfx = appRoot.GameplayWorldClickSfx;
            gameplayWorldClickSfx.Construct(
                audioService,
                gameplayPointerInput);
            Camera worldCamera = Camera.main;

            worldCamera
                .GetComponent<GameplayCameraSafeAreaFitter>()
                .SetTopRoot(appRoot.GameplayTopRoot);

            appRoot.GameplayPoolRoot.Initialize();
            RequiredPackageMatcher requiredPackageMatcher =
                new RequiredPackageMatcher();
            RequiredPackageGenerator requiredPackageGenerator = new();
            RequiredPackageLifecycleUseCase requiredPackageLifecycleUseCase =
                new RequiredPackageLifecycleUseCase(
                    requiredPackageGenerator,
                    requiredPackageMatcher);
            SelectFoodUseCase selectFoodUseCase =
                new SelectFoodUseCase(requiredPackageMatcher);
            BoardModelFactory boardModelFactory = new();

            IGameBoosterConfig boosterConfig = configurationSession;
            BoosterManager boosterManager = new(
                playerProfileService,
                boosterConfig);
            IGameEconomyConfig economyConfig = configurationSession;
            IGameAdsConfig adsConfig = configurationSession;
            IStorePaymentGateway storePaymentGateway =
                new DebugFreeStorePaymentGateway();
            ShopPurchaseService shopPurchaseService = new(
                shopConfig,
                storePaymentGateway,
                playerProfileService);
            GoldPassPurchaseService goldPassPurchaseService = new(
                goldPassConfig,
                storePaymentGateway,
                goldPassService);
            IAddressableUiFactory addressableUiFactory =
                new AddressableUiFactory();

            appRoot.UIManager.Construct(
                GameplayEvents,
                audioService,
                boosterManager,
                boosterConfig,
                economyConfig,
                goldPassProgressionConfig,
                advertisingRuntimeSettings,
                playerProfileService,
                goldPassService,
                levelCatalogRepository,
                shopConfig,
                addressableUiFactory,
                appRoot.GameplayPoolRoot.ComboFeedback,
                appRoot.GameplayPoolRoot.ClickParticles,
                appRoot.GameplayPointerInput);
            CreateAdServices(
                appRoot,
                advertisingRuntimeSettings,
                out IRewardedAdService rewardedAdService,
                out IInterstitialAdService interstitialAdService);
            PostLevelAdCooldown postLevelAdCooldown = new(
                saveService,
                clock);
            PostLevelAdCoordinator postLevelAdCoordinator = new(
                interstitialAdService,
                adsConfig,
                advertisingRuntimeSettings,
                postLevelAdCooldown,
                playerProfileService);
            appRoot.BoardLayoutView.Construct(
                appRoot.FoodVisualResolver,
                appRoot.GameplayPoolRoot.FoodItems,
                appRoot.GameplayPoolRoot.Grills,
                appRoot.GameplayPoolRoot.Trays,
                worldCamera);
            appRoot.WaitingRackView.Construct(
                appRoot.GameplayPoolRoot.FoodItems);
            appRoot.RequiredPackageGroupView.Construct(
                appRoot.GameplayPoolRoot.PackageCompleteBursts);
            appRoot.GameplayMotionPresenter.Construct(
                appRoot.RequiredPackageGroupView,
                appRoot.WaitingRackView);
            appRoot.GameplayController.Construct(
                appRoot.UIManager,
                GameplayEvents,
                appRoot.BoardLayoutView,
                appRoot.RequiredPackageGroupView,
                appRoot.WaitingRackView,
                appRoot.FridgeBoosterAnchors,
                appRoot.GameplayMotionPresenter,
                gameplayAudioPresenter,
                gameplayPointerInput,
                gameplayWorldClickSfx,
                appRoot.FoodVisualResolver,
                appRoot.GameplayPoolRoot.FoodItems,
                requiredPackageLifecycleUseCase,
                selectFoodUseCase,
                boardModelFactory);
            appRoot.AppController.Construct(
                appRoot.UIManager,
                appRoot.GameplayController,
                playerProfileService,
                goldPassService,
                goldPassProgressionConfig,
                boosterManager,
                economyConfig,
                shopPurchaseService,
                goldPassPurchaseService,
                rewardedAdService,
                postLevelAdCoordinator,
                levelCatalogRepository,
                levelRepository,
                LevelSynchronizer,
                audioService);

            return true;
        }

        private void CreateAdServices(
            AppRoot appRoot,
            IAdvertisingRuntimeSettings runtimeSettings,
            out IRewardedAdService rewardedAdService,
            out IInterstitialAdService interstitialAdService)
        {
#if UNITY_ANDROID || UNITY_EDITOR
            if (runtimeSettings.UseLevelPlayAds)
            {
                LevelPlayAdSettings adSettings = CreateLevelPlayAdSettings();
                LevelPlayAdsInitializer adsInitializer =
                    new(adSettings.AppKey);
                rewardedAdService = new LevelPlayRewardedAdService(
                    adsInitializer,
                    adSettings.RewardedAdUnitId);
                interstitialAdService = new LevelPlayInterstitialAdService(
                    adsInitializer,
                    adSettings.InterstitialAdUnitId);
                adsInitializer.Initialize();
                return;
            }
#endif

            rewardedAdService =
                new FakeRewardedAdService(appRoot.UIManager);
            interstitialAdService =
                new FakeInterstitialAdService(appRoot.UIManager);
        }

        private LevelPlayAdSettings CreateLevelPlayAdSettings()
        {
            LevelPlayMediationSettings mediationSettings =
                Resources.Load<LevelPlayMediationSettings>(
                    "LevelPlayMediationSettings");

            return new LevelPlayAdSettings(
                mediationSettings.AndroidAppKey,
                _rewardedAdUnitId,
                _interstitialAdUnitId);
        }

        private static bool TryCreateShopConfig(out IGameShopConfig shopConfig)
        {
            ResourcesGameShopConfigLoader loader = new();

            if (loader.TryLoad(out shopConfig, out string errorMessage))
            {
                return true;
            }

            Debug.LogError($"Cannot install app because shop config is invalid: {errorMessage}");
            shopConfig = null;
            return false;
        }

        private static bool TryCreateGoldPassConfig(
            out IGameGoldPassConfig goldPassConfig)
        {
            ResourcesGameGoldPassConfigLoader loader = new();

            if (loader.TryLoad(out goldPassConfig, out string errorMessage))
            {
                return true;
            }

            Debug.LogError(
                $"Cannot install app because Gold Pass config is invalid: " +
                errorMessage);
            goldPassConfig = null;
            return false;
        }

        private static void LogPlayerProfileSaveFailure(string errorMessage)
        {
            Debug.LogError($"Player profile save failed: {errorMessage}");
        }

        private static bool TryLoadBundledLevelData(
            out ResourcesLevelCatalogData catalogData,
            out LevelContentValidator levelContentValidator)
        {
            PackageSelectionSettingsValidator packageSelectionValidator = new();
            LevelRandomSettingsValidator randomSettingsValidator = new();
            GrillLayoutValidator grillLayoutValidator = new();
            GrillMovementGroupValidator grillMovementGroupValidator = new();
            LevelValidator levelValidator = new(
                packageSelectionValidator,
                randomSettingsValidator,
                grillLayoutValidator,
                grillMovementGroupValidator);
            ResourcesLevelCatalogLoader loader = new(
                new LevelCatalogJsonParser(),
                new LevelCatalogValidator(),
                new LevelCatalogMapper());

            if (!loader.TryLoad(
                    out catalogData,
                    out LevelValidationResult validationResult))
            {
                LogLevelValidation(validationResult);
                levelContentValidator = null;
                return false;
            }

            LogLevelValidation(validationResult);
            levelContentValidator =
                new LevelContentValidator(levelValidator);
            return true;
        }

        private static void LogLevelValidation(LevelValidationResult validationResult)
        {
            for (int i = 0; i < validationResult.Errors.Count; i++)
            {
                Debug.LogError(validationResult.Errors[i]);
            }

            for (int i = 0; i < validationResult.Warnings.Count; i++)
            {
                Debug.LogWarning(validationResult.Warnings[i]);
            }
        }

        private static IAudioService CreateAudioService(
            AppRoot appRoot,
            ISaveService saveService)
        {
            appRoot.AudioService.Construct(saveService);
            return appRoot.AudioService;
        }

    }
}
