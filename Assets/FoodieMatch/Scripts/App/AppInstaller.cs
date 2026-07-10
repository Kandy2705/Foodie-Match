using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Core.Infrastructure.Save;
using FoodieMatch.Data.Level;
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

            IAudioService audioService = new NullAudioService();
            ISaveService saveService = new PlayerPrefsSaveServiceAdapter();
            RequiredPackageMatcher requiredPackageMatcher =
                new RequiredPackageMatcher();
            SelectFoodUseCase selectFoodUseCase =
                new SelectFoodUseCase(requiredPackageMatcher);
            LevelDataMapper levelDataMapper = new LevelDataMapper();
            ILevelRepository levelRepository =
                new ScriptableObjectLevelRepository(
                    appRoot.LevelCatalog,
                    levelDataMapper);
            BoardModelFactory boardModelFactory =
                new BoardModelFactory();

            appRoot.UIManager.Construct(GameplayEvents, audioService);
            appRoot.BoardLayoutView.Construct(
                appRoot.FoodVisualResolver);
            appRoot.GameplayController.Construct(
                appRoot.UIManager,
                GameplayEvents,
                appRoot.BoardLayoutView,
                appRoot.RequiredPackageGroupView,
                appRoot.WaitingRackView,
                appRoot.FoodVisualResolver,
                selectFoodUseCase,
                levelRepository,
                boardModelFactory);
            appRoot.AppController.Construct(
                appRoot.UIManager,
                appRoot.GameplayController,
                saveService,
                levelRepository);
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
