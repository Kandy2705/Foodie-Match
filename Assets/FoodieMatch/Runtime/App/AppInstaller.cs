using FoodieMatch.Runtime.Core.Application.Events;
using FoodieMatch.Runtime.Core.Infrastructure;
using FoodieMatch.Runtime.Core.Infrastructure.Audio;
using FoodieMatch.Runtime.Core.Infrastructure.Save;
using UnityEngine;
namespace FoodieMatch.Runtime.App
{
    public sealed class AppInstaller : MonoBehaviour
    {
        public GameplayEvents GameplayEvents { get; private set; }
        public ServiceLocator ServiceLocator { get; private set; }

        public void Install(AppRoot appRoot)
        {
            if (!HasValidReferences(appRoot))
            {
                return;
            }

            GameplayEvents = new GameplayEvents();
            ServiceLocator = new ServiceLocator();

            RegisterInfrastructureServices();

            IAudioService audioService = ServiceLocator.Get<IAudioService>();
            ISaveService saveService = ServiceLocator.Get<ISaveService>();

            appRoot.UIManager.Construct(GameplayEvents, audioService);
            appRoot.GameplayController.Construct(appRoot, appRoot.UIManager, GameplayEvents);
            appRoot.AppController.Construct(appRoot.UIManager, appRoot.GameplayController, saveService);
        }

        private void RegisterInfrastructureServices()
        {
            IAudioService audioService = new NullAudioService();
            ISaveService saveService = new PlayerPrefsSaveServiceAdapter();

            ServiceLocator.Register(audioService);
            ServiceLocator.Register(saveService);
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

            return true;
        }
    }
}
