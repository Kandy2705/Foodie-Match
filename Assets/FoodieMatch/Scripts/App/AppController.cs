using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Infrastructure.Audio;
using FoodieMatch.Data.Booster;
using FoodieMatch.Features.Gameplay;
using FoodieMatch.UI;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppController : MonoBehaviour
    {
        private UIManager _uiManager;
        private GameplayController _gameplayController;
        private PlayerProfileService _playerProfileService;
        private BoosterManager _boosterManager;
        private ILevelRepository _levelRepository;
        private IAudioService _audioService;
        private GameplayNavigationActions _gameplayNavigationActions;
        private bool _isTransitionRunning;
        private bool _isDestroyed;
        private int _activeLevelNumber;

        private void OnDestroy()
        {
            _isDestroyed = true;

            if (_uiManager == null)
            {
                return;
            }

            _uiManager.PlayGameRequested -= OnPlayGameRequested;
            _uiManager.LeaveGameRequested -= OnLeaveGameRequested;
            _uiManager.RestartGameRequested -= OnRestartGameRequested;
            _uiManager.BoosterUseHandler = null;
            _uiManager.HideLoading();
        }

        public void Construct(
            UIManager uiManager,
            GameplayController gameplayController,
            PlayerProfileService playerProfileService,
            BoosterManager boosterManager,
            ILevelRepository levelRepository,
            IAudioService audioService)
        {
            _uiManager = uiManager;
            _gameplayController = gameplayController;
            _playerProfileService = playerProfileService;
            _boosterManager = boosterManager;
            _levelRepository = levelRepository;
            _audioService = audioService;
            _gameplayNavigationActions = new(
                OnGameplayHomeRequested,
                OnGameplayRetryRequested,
                OnWinRewardClaimed);

            _uiManager.PlayGameRequested -= OnPlayGameRequested;
            _uiManager.PlayGameRequested += OnPlayGameRequested;
            _uiManager.LeaveGameRequested -= OnLeaveGameRequested;
            _uiManager.LeaveGameRequested += OnLeaveGameRequested;
            _uiManager.RestartGameRequested -= OnRestartGameRequested;
            _uiManager.RestartGameRequested += OnRestartGameRequested;
            _uiManager.BoosterUseHandler = OnBoosterUseRequested;
        }

        public void EnterHome()
        {
            if (!HasDependencies())
            {
                return;
            }

            int levelNumber = GetSavedPlayableLevelNumber();
            OpenHome(levelNumber);
        }

        public void StartLevel(int levelNumber)
        {
            if (!CanStartLevel(levelNumber))
            {
                return;
            }

            _ = EnterLevelWithLoadingSafelyAsync(levelNumber);
        }

        public void BackToHome()
        {
            if (!HasDependencies())
            {
                return;
            }

            _ = EnterHomeWithLoadingSafelyAsync(GetSavedPlayableLevelNumber());
        }

        private async Task EnterLevelWithLoadingSafelyAsync(int levelNumber)
        {
            if (!TryBeginTransition())
            {
                return;
            }

            try
            {
                Task loadingTask = _uiManager.PlayLoadingAsync();
                await Task.Yield();

                if (!_isDestroyed)
                {
                    OpenLevel(levelNumber);
                }

                await loadingTask;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                FinishTransition();
            }
        }

        private async Task EnterHomeWithLoadingSafelyAsync(int levelNumber)
        {
            if (!TryBeginTransition())
            {
                return;
            }

            try
            {
                Task loadingTask = _uiManager.PlayLoadingAsync();
                await Task.Yield();

                if (!_isDestroyed)
                {
                    OpenHome(levelNumber);
                }

                await loadingTask;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                FinishTransition();
            }
        }

        private void OpenLevel(int levelNumber)
        {
            _gameplayController.ClearLevel();
            _uiManager.HideAllPopups();
            _uiManager.HideHome();
            _uiManager.SetCurrentLevelNumber(levelNumber);
            _uiManager.ShowGameplayHud();
            _audioService?.PlayMusic(AudioKeys.MusicIngame);

            _playerProfileService.SetCurrentLevelNumber(levelNumber);
            _activeLevelNumber = levelNumber;
            _gameplayController.StartLevel(levelNumber, _gameplayNavigationActions);
        }

        private void OpenHome(int levelNumber)
        {
            _gameplayController.ClearLevel();
            _uiManager.HideAllPopups();
            _uiManager.HideGameplayHud();
            _uiManager.SetCurrentLevelNumber(levelNumber);
            _uiManager.ShowHome();
            _audioService?.PlayMusic(AudioKeys.MusicMenu);
            _activeLevelNumber = 0;
        }

        private bool TryBeginTransition()
        {
            if (_isTransitionRunning || _isDestroyed)
            {
                return false;
            }

            _isTransitionRunning = true;
            return true;
        }

        private void FinishTransition()
        {
            if (!_isDestroyed)
            {
                _uiManager.HideLoading();
            }

            _isTransitionRunning = false;
        }

        private bool CanStartLevel(int levelNumber)
        {
            if (!HasDependencies())
            {
                return false;
            }

            if (_levelRepository.TryGetLevel(levelNumber, out _))
            {
                return true;
            }

            Debug.LogError($"Level {levelNumber} could not be loaded.");
            return false;
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

            if (_playerProfileService == null)
            {
                Debug.LogError("PlayerProfileService has not been constructed.");
                return false;
            }

            if (_boosterManager == null)
            {
                Debug.LogError("BoosterManager has not been constructed.");
                return false;
            }

            if (_levelRepository == null)
            {
                Debug.LogError("LevelRepository has not been constructed.");
                return false;
            }

            return true;
        }

        private int GetSavedPlayableLevelNumber()
        {
            int savedLevelNumber = _playerProfileService.CurrentLevelNumber;

            if (_levelRepository.TryGetLevel(savedLevelNumber, out _))
            {
                return savedLevelNumber;
            }

            if (_levelRepository.TryGetFirstLevel(out _))
            {
                return 1;
            }

            Debug.LogError("Level catalog does not contain a playable level.");
            return 0;
        }

        private void OnPlayGameRequested()
        {
            int levelNumber = GetSavedPlayableLevelNumber();

            if (levelNumber > 0)
            {
                StartLevel(levelNumber);
            }
        }

        private bool OnBoosterUseRequested(BoosterType boosterType)
        {
            if (_gameplayController == null || _boosterManager == null)
            {
                return false;
            }

            if (!_boosterManager.TryUse(boosterType))
            {
                return false;
            }

            try
            {
                if (_gameplayController.TryApplyBooster(boosterType))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            _boosterManager.Add(boosterType, amount: 1);
            return false;
        }

        private void OnLeaveGameRequested()
        {
            BackToHome();
        }

        private void OnRestartGameRequested()
        {
            if (_activeLevelNumber > 0)
            {
                StartLevel(_activeLevelNumber);
            }
        }

        private void OnGameplayHomeRequested()
        {
            BackToHome();
        }

        private void OnGameplayRetryRequested(int levelNumber)
        {
            StartLevel(levelNumber);
        }

        private void OnWinRewardClaimed(int completedLevelNumber)
        {
            int homeLevelNumber = completedLevelNumber;

            if (_levelRepository.TryGetNextLevel(completedLevelNumber, out _))
            {
                homeLevelNumber++;
            }

            _playerProfileService.SetCurrentLevelNumber(homeLevelNumber);
            _ = EnterHomeWithLoadingSafelyAsync(homeLevelNumber);
        }
    }
}
