using FoodieMatch.Runtime.Core.Infrastructure.Save;
using FoodieMatch.Runtime.Features.LevelSystem;
using FoodieMatch.Runtime.UI;
using UnityEngine;
namespace FoodieMatch.Runtime.App
{
    public sealed class AppController : MonoBehaviour
    {
        private const string LastLevelIdKey = "LastLevelId";
        private UIManager _uiManager;
        private GameplayController _gameplayController;
        private ISaveService _saveService;

        public void Construct(
            UIManager uiManager,
            GameplayController gameplayController,
            ISaveService saveService)
        {
            _uiManager = uiManager;
            _gameplayController = gameplayController;
            _saveService = saveService;
        }

        public void EnterHome()
        {
            if (!HasDependencies())
            {
                return;
            }

            _uiManager.HideGameplayHud();
            _uiManager.ShowHome();
        }

        public void StartLevel(int levelId)
        {
            if (!HasDependencies())
            {
                return;
            }

            _uiManager.HideAllPopups();
            _uiManager.ShowGameplayHud();

            _saveService.SetInt(LastLevelIdKey, levelId);
            _saveService.Save();

            _gameplayController.StartLevel(levelId, BackToHome);
        }

        public void BackToHome()
        {
            if (!HasDependencies())
            {
                return;
            }

            _gameplayController.ClearLevel();
            EnterHome();
        }

        private bool HasDependencies()
        {
            if (_uiManager == null)
            {
                Debug.LogError("UIManager has not been constructed.");
                return false;
            }

            if (_gameplayController == null)
            {
                Debug.LogError("GameplayController has not been constructed.");
                return false;
            }

            if (_saveService == null)
            {
                Debug.LogError("SaveService has not been constructed.");
                return false;
            }
            return true;
        }
    }
}
