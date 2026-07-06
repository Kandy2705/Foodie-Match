using FoodieMatch.Core.Infrastructure.Save;
using FoodieMatch.Features.LevelSystem;
using FoodieMatch.UI;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppController : MonoBehaviour
    {
        private const int DefaultLevelId = 1;
        private const string LastLevelIdKey = "LastLevelId";
        private UIManager _uiManager;
        private GameplayController _gameplayController;
        private ISaveService _saveService;

        private void OnDestroy()
        {
            if (_uiManager == null)
            {
                return;
            }

            _uiManager.PlayGameRequested -= OnPlayGameRequested;
        }

        public void Construct(
            UIManager uiManager,
            GameplayController gameplayController,
            ISaveService saveService)
        {
            _uiManager = uiManager;
            _gameplayController = gameplayController;
            _saveService = saveService;

            _uiManager.PlayGameRequested -= OnPlayGameRequested;
            _uiManager.PlayGameRequested += OnPlayGameRequested;
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
            _uiManager.HideHome();
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

        private void OnPlayGameRequested()
        {
            StartLevel(DefaultLevelId);
        }
    }
}
