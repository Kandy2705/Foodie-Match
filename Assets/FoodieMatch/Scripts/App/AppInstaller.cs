using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Core.Infrastructure.Save;
using FoodieMatch.Data.Booster;
using FoodieMatch.Data.Level;
using FoodieMatch.Features.Gameplay;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppInstaller : MonoBehaviour
    {
        public GameplayEvents GameplayEvents { get; private set; }

        public void Install(AppRoot appRoot)
        {
            if (!HasValidReferences(appRoot))
            {
                return;
            }

            GameplayEvents = new GameplayEvents();

            ISaveService saveService = new PlayerPrefsSaveServiceAdapter();
            IAudioService audioService = CreateAudioService(appRoot, saveService);
            GameplayAudioPresenter gameplayAudioPresenter = new(audioService);
            GameplayWorldClickSfx gameplayWorldClickSfx = CreateGameplayWorldClickSfx(appRoot, audioService);
            RequiredPackageMatcher requiredPackageMatcher =
                new RequiredPackageMatcher();
            System.Random random = new System.Random();
            RequiredPackageGenerator requiredPackageGenerator =
                new RequiredPackageGenerator(random);
            RequiredPackageLifecycleUseCase requiredPackageLifecycleUseCase =
                new RequiredPackageLifecycleUseCase(
                    requiredPackageGenerator,
                    requiredPackageMatcher);
            SelectFoodUseCase selectFoodUseCase =
                new SelectFoodUseCase(requiredPackageMatcher);
            LevelDataMapper levelDataMapper = new LevelDataMapper();
            ILevelRepository levelRepository =
                new ScriptableObjectLevelRepository(
                    appRoot.LevelCatalog,
                    levelDataMapper);
            BoardModelFactory boardModelFactory =
                new BoardModelFactory();

            BoosterManager boosterManager = new BoosterManager(
                saveService,
                new int[] { 2, 0, 1, 0 });

            appRoot.UIManager.Construct(
                GameplayEvents,
                audioService,
                boosterManager,
                saveService);
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
                saveService,
                levelRepository,
                audioService);
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
