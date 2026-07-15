using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Core.Infrastructure.Save;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.UI;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppController : MonoBehaviour
    {
        private const string CurrentLevelNumberKey = "CurrentLevelNumber";
        private UIManager _uiManager;
        private GameplayController _gameplayController;
        private ISaveService _saveService;
        private ILevelRepository _levelRepository;
        private IAudioService _audioService;

        private void OnDestroy()
        {
            if (_uiManager == null)
            {
                return;
            }

            _uiManager.PlayGameRequested -= OnPlayGameRequested;
            _uiManager.LeaveGameRequested -= OnLeaveGameRequested;
        }

        public void Construct(
            UIManager uiManager,
            GameplayController gameplayController,
            ISaveService saveService,
            ILevelRepository levelRepository,
            IAudioService audioService)
        {
            _uiManager = uiManager;
            _gameplayController = gameplayController;
            _saveService = saveService;
            _levelRepository = levelRepository;
            _audioService = audioService;

            _uiManager.PlayGameRequested -= OnPlayGameRequested;
            _uiManager.PlayGameRequested += OnPlayGameRequested;

            _uiManager.LeaveGameRequested -= OnLeaveGameRequested;
            _uiManager.LeaveGameRequested += OnLeaveGameRequested;
        }

        public void EnterHome()
        {
            if (!HasDependencies())
            {
                return;
            }

            _uiManager.HideGameplayHud();
            _uiManager.ShowHome();
            _audioService?.PlayMusic(AudioKeys.MusicMenu);
        }

        public void StartLevel(int levelNumber)
        {
            if (!HasDependencies())
            {
                return;
            }

            if (!_levelRepository.TryGetLevel(levelNumber, out _))
            {
                Debug.LogError($"Level {levelNumber} could not be loaded.");
                return;
            }

            _uiManager.HideAllPopups();
            _uiManager.HideHome();
            _uiManager.ShowGameplayHud();
            _audioService?.PlayMusic(AudioKeys.MusicIngame);

            _saveService.SetInt(CurrentLevelNumberKey, levelNumber);
            _saveService.Save();

            _gameplayController.StartLevel(levelNumber, BackToHome);
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

            if (_levelRepository == null)
            {
                Debug.LogError("LevelRepository has not been constructed.");
                return false;
            }

            return true;
        }

        private void OnPlayGameRequested()
        {
            if (_saveService.HasKey(CurrentLevelNumberKey))
            {
                int savedLevelNumber =
                    _saveService.GetInt(CurrentLevelNumberKey, 0);

                if (_levelRepository.TryGetLevel(
                        savedLevelNumber,
                        out _))
                {
                    StartLevel(savedLevelNumber);
                    return;
                }
            }

            if (!_levelRepository.TryGetFirstLevel(out _))
            {
                Debug.LogError("Level catalog does not contain a playable level.");
                return;
            }

            StartLevel(1);
        }

        private void OnLeaveGameRequested()
        {
            BackToHome();
        }
    }
}
