using FoodieMatch.App.Advertising;
using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Audio;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.Player;
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
using FoodieMatch.Infrastructure.Level;
using FoodieMatch.Infrastructure.Level.Json;
using FoodieMatch.Infrastructure.Persistence.PlayerProfiles;
using FoodieMatch.Infrastructure.Persistence.Save;
using FoodieMatch.Infrastructure.Shop;
using FoodieMatch.Infrastructure.Time;
using FoodieMatch.UI.Advertising;
using FoodieMatch.UI.AddressableAssets;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppInstaller : MonoBehaviour
    {
        [Header("Advertising")]
        [SerializeField] private string _rewardedAdUnitId;
        [SerializeField] private string _interstitialAdUnitId;

        public GameplayEvents GameplayEvents { get; private set; }

        public PlayerProfileInitializer PlayerProfileInitializer { get; private set; }

        public bool Install(AppRoot appRoot)
        {
            if (!TryCreateLevelRepository(out ILevelRepository levelRepository))
            {
                return false;
            }

            if (!TryCreateShopConfig(out IGameShopConfig shopConfig))
            {
                return false;
            }

            GameplayEvents = new GameplayEvents();

            ISaveService saveService = new PlayerPrefsSaveServiceAdapter();
            IAdvertisingRuntimeSettings advertisingRuntimeSettings =
                new PlayerPrefsAdvertisingRuntimeSettings(saveService);
            IGameHeartConfig heartConfig =
                GameHeartDefaults.CreateSnapshot();
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
                heartConfig);
            PlayerProfileService playerProfileService = new(
                profileRepository,
                profileSession,
                heartConfig,
                clock);
            playerProfileService.SaveFailed += LogPlayerProfileSaveFailure;
            IAudioService audioService = CreateAudioService(appRoot, saveService);
            GameplayAudioPresenter gameplayAudioPresenter = new(audioService);
            GameplayWorldClickSfx gameplayWorldClickSfx = appRoot.GameplayWorldClickSfx;
            gameplayWorldClickSfx.Construct(audioService);
            Camera worldCamera = Camera.main;

            if (worldCamera == null)
            {
                Debug.LogError(
                    "Cannot install app because Main Camera is missing.");
                return false;
            }

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

            BoosterManager boosterManager = new(playerProfileService);
            IGameBoosterConfig boosterConfig =
                GameBoosterDefaults.CreateSnapshot();
            IGameEconomyConfig economyConfig =
                GameEconomyDefaults.CreateSnapshot();
            IGameAdsConfig adsConfig = GameAdsDefaults.CreateSnapshot();
            ShopPurchaseService shopPurchaseService = new(
                shopConfig,
                new DebugFreeShopPaymentGateway(),
                playerProfileService);
            IAddressableUiFactory addressableUiFactory =
                new AddressableUiFactory();

            appRoot.UIManager.Construct(
                GameplayEvents,
                audioService,
                boosterManager,
                boosterConfig,
                economyConfig,
                advertisingRuntimeSettings,
                playerProfileService,
                levelRepository,
                shopConfig,
                addressableUiFactory);
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
                postLevelAdCooldown);
            appRoot.BoardLayoutView.Construct(
                appRoot.FoodVisualResolver,
                appRoot.GameplayPoolRoot.FoodItems,
                appRoot.GameplayPoolRoot.Grills,
                appRoot.GameplayPoolRoot.Trays,
                worldCamera);
            appRoot.WaitingRackView.Construct(
                appRoot.GameplayPoolRoot.FoodItems);
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
                gameplayWorldClickSfx,
                appRoot.FoodVisualResolver,
                appRoot.GameplayPoolRoot.FoodItems,
                requiredPackageLifecycleUseCase,
                selectFoodUseCase,
                levelRepository,
                boardModelFactory);
            appRoot.AppController.Construct(
                appRoot.UIManager,
                appRoot.GameplayController,
                playerProfileService,
                boosterManager,
                economyConfig,
                shopPurchaseService,
                rewardedAdService,
                postLevelAdCoordinator,
                levelRepository,
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

        private static void LogPlayerProfileSaveFailure(string errorMessage)
        {
            Debug.LogError($"Player profile save failed: {errorMessage}");
        }

        private static bool TryCreateLevelRepository(out ILevelRepository levelRepository)
        {
            LevelCatalogJsonParser parser = new();
            PackageSelectionSettingsValidator packageSelectionValidator = new();
            LevelRandomSettingsValidator randomSettingsValidator = new();
            GrillLayoutValidator grillLayoutValidator = new();
            GrillMovementGroupValidator grillMovementGroupValidator = new();
            LevelValidator levelValidator = new(
                packageSelectionValidator,
                randomSettingsValidator,
                grillLayoutValidator,
                grillMovementGroupValidator);
            LevelCatalogValidator catalogValidator = new(levelValidator);
            LevelCatalogMapper mapper = new();
            ResourcesLevelCatalogLoader loader = new(parser, catalogValidator, mapper);

            if (!loader.TryLoad(out LevelCatalog catalog, out LevelValidationResult validationResult))
            {
                LogLevelValidation(validationResult);
                levelRepository = null;
                return false;
            }

            LogLevelValidation(validationResult);
            levelRepository = new LevelCatalogRepository(catalog);
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
