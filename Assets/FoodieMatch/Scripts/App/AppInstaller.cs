using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Audio;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.Time;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.Infrastructure.Audio;
using FoodieMatch.Infrastructure.Level;
using FoodieMatch.Infrastructure.Level.Json;
using FoodieMatch.Infrastructure.Persistence.PlayerProfiles;
using FoodieMatch.Infrastructure.Persistence.Save;
using FoodieMatch.Infrastructure.Time;
using FoodieMatch.UI.Advertising;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppInstaller : MonoBehaviour
    {
        public GameplayEvents GameplayEvents { get; private set; }

        public PlayerProfileInitializer PlayerProfileInitializer { get; private set; }

        public bool Install(AppRoot appRoot)
        {
            if (!HasValidReferences(appRoot))
            {
                return false;
            }

            if (!TryCreateLevelRepository(out ILevelRepository levelRepository))
            {
                return false;
            }

            GameplayEvents = new GameplayEvents();

            ISaveService saveService = new PlayerPrefsSaveServiceAdapter();
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
            GameplayWorldClickSfx gameplayWorldClickSfx = CreateGameplayWorldClickSfx(appRoot, audioService);
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

            appRoot.UIManager.Construct(
                GameplayEvents,
                audioService,
                boosterManager,
                boosterConfig,
                economyConfig,
                playerProfileService);
            IRewardedAdService rewardedAdService =
                new FakeRewardedAdService(appRoot.UIManager);
            appRoot.BoardLayoutView.Construct(
                appRoot.FoodVisualResolver);
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
                rewardedAdService,
                levelRepository,
                audioService);

            return true;
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
            if (appRoot.AudioService == null)
            {
                Debug.LogWarning(
                    "UnityAudioService is missing. Falling back to NullAudioService.");
                return new NullAudioService();
            }

            appRoot.AudioService.Construct(saveService);
            return appRoot.AudioService;
        }

        private static GameplayWorldClickSfx CreateGameplayWorldClickSfx(
            AppRoot appRoot,
            IAudioService audioService)
        {
            GameplayWorldClickSfx worldClickSfx =
                appRoot.GameplayController.GetComponent<GameplayWorldClickSfx>();

            if (worldClickSfx == null)
            {
                worldClickSfx = appRoot.GameplayController.gameObject.AddComponent<GameplayWorldClickSfx>();
            }

            worldClickSfx.Construct(audioService);
            return worldClickSfx;
        }

        private bool HasValidReferences(AppRoot appRoot)
        {
            if (appRoot == null)
            {
                Debug.LogError("Cannot install app because AppRoot is null.");
                return false;
            }

            if (appRoot.AppController == null)
            {
                Debug.LogError("Cannot install app because AppController is missing.");
                return false;
            }

            if (appRoot.GameplayController == null)
            {
                Debug.LogError("Cannot install app because GameplayController is missing.");
                return false;
            }

            if (appRoot.UIManager == null)
            {
                Debug.LogError("Cannot install app because UIManager is missing.");
                return false;
            }

            if (appRoot.GameplayMotionPresenter == null)
            {
                Debug.LogError(
                    "Cannot install app because GameplayMotionPresenter is missing.");
                return false;
            }

            if (appRoot.BoardLayoutView == null)
            {
                Debug.LogError("Cannot install app because BoardLayoutView is missing.");
                return false;
            }

            if (appRoot.RequiredPackageGroupView == null)
            {
                Debug.LogError("Cannot install app because RequiredPackageGroupView is missing.");
                return false;
            }

            if (appRoot.WaitingRackView == null)
            {
                Debug.LogError("Cannot install app because WaitingRackView is missing.");
                return false;
            }

            if (appRoot.FoodVisualResolver == null)
            {
                Debug.LogError("Cannot install app because FoodVisualResolver is missing.");
                return false;
            }

            return true;
        }
    }
}
