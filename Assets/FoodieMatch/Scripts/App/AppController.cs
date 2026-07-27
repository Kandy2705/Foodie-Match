using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Audio;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Booster;
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
        private IGameEconomyConfig _economyConfig;
        private IRewardedAdService _rewardedAdService;
        private ILevelRepository _levelRepository;
        private IAudioService _audioService;
        private GameplayNavigationActions _gameplayNavigationActions;
        private bool _isTransitionRunning;
        private int _activeLevelNumber;

        public void Construct(
            UIManager uiManager,
            GameplayController gameplayController,
            PlayerProfileService playerProfileService,
            BoosterManager boosterManager,
            IGameEconomyConfig economyConfig,
            IRewardedAdService rewardedAdService,
            ILevelRepository levelRepository,
            IAudioService audioService)
        {
            _uiManager = uiManager;
            _gameplayController = gameplayController;
            _playerProfileService = playerProfileService;
            _boosterManager = boosterManager;
            _economyConfig = economyConfig;
            _rewardedAdService = rewardedAdService;
            _levelRepository = levelRepository;
            _audioService = audioService;
            _gameplayNavigationActions = new(
                OnGameplayHomeRequested,
                OnGameplayRetryRequested,
                OnGameplayLevelLost,
                OnGameplayLevelWon);

            _uiManager.PlayGameRequested += OnPlayGameRequested;
            _uiManager.LeaveGameRequested += OnLeaveGameRequested;
            _uiManager.BoosterCoinPurchaseRequested += OnBoosterCoinPurchaseRequested;
            _uiManager.BoosterRewardedAdRequested += OnBoosterRewardedAdRequested;
            _uiManager.BoosterUseHandler = OnBoosterUseRequested;
            _uiManager.RestartGameHandler = OnRestartGameRequested;
        }

        public void EnterHome()
        {
            int levelNumber = GetSavedPlayableLevelNumber();
            OpenHome(levelNumber, _playerProfileService.CoinBalance);
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
                OpenLevel(levelNumber);
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

        private async Task EnterHomeWithLoadingSafelyAsync(
            int levelNumber,
            HomeCoinRewardPresentation coinRewardPresentation = null)
        {
            if (!TryBeginTransition())
            {
                return;
            }

            await RunHomeTransitionSafelyAsync(
                levelNumber,
                coinRewardPresentation);
        }

        private async Task RunHomeTransitionSafelyAsync(
            int levelNumber,
            HomeCoinRewardPresentation coinRewardPresentation)
        {
            bool shouldPlayCoinReward = false;

            try
            {
                Task loadingTask = _uiManager.PlayLoadingAsync();
                await Task.Yield();
                long displayedCoinBalance = coinRewardPresentation == null
                    ? _playerProfileService.CoinBalance
                    : coinRewardPresentation.StartingCoinBalance;
                OpenHome(levelNumber, displayedCoinBalance);
                shouldPlayCoinReward = coinRewardPresentation != null;

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

            if (shouldPlayCoinReward)
            {
                _uiManager.PlayHomeCoinReward(
                    coinRewardPresentation.StartingCoinBalance,
                    coinRewardPresentation.TargetCoinBalance,
                    coinRewardPresentation.CoinValuePerImage);
            }
        }

        private void OpenLevel(int levelNumber)
        {
            _gameplayController.ClearLevel();
            _uiManager.HideAllPopups();
            _uiManager.HideHome();
            _uiManager.SetCurrentLevelNumber(levelNumber);
            _uiManager.ShowGameplayHud();
            _audioService.PlayMusic(AudioKeys.MusicIngame);

            _playerProfileService.SetCurrentLevelNumber(levelNumber);
            _activeLevelNumber = levelNumber;
            _gameplayController.StartLevel(levelNumber, _gameplayNavigationActions);
        }

        private void OpenHome(
            int levelNumber,
            long displayedCoinBalance)
        {
            _gameplayController.ClearLevel();
            _uiManager.HideAllPopups();
            _uiManager.HideGameplayHud();
            _uiManager.SetCurrentLevelNumber(levelNumber);
            _uiManager.ShowHome(displayedCoinBalance);
            _audioService.PlayMusic(AudioKeys.MusicMenu);
            _activeLevelNumber = 0;
        }

        private bool TryBeginTransition()
        {
            if (_isTransitionRunning)
            {
                return false;
            }

            _isTransitionRunning = true;
            return true;
        }

        private void FinishTransition()
        {
            _uiManager.HideLoading();
            _isTransitionRunning = false;
        }

        private bool CanStartLevel(int levelNumber)
        {
            if (!CanLoadLevel(levelNumber))
            {
                return false;
            }

            return _playerProfileService.HasAvailableHeart();
        }

        private bool CanLoadLevel(int levelNumber)
        {
            if (_levelRepository.TryGetLevel(levelNumber, out _))
            {
                return true;
            }

            Debug.LogError($"Level {levelNumber} could not be loaded.");
            return false;
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

        private void OnBoosterCoinPurchaseRequested(BoosterType boosterType)
        {
            try
            {
                int coinPrice = _economyConfig.GetBoosterPrice(boosterType);

                if (!_boosterManager.TryPurchase(boosterType, coinPrice))
                {
                    return;
                }

                UpdateUiAfterBoosterGranted(boosterType);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnBoosterRewardedAdRequested(BoosterType boosterType)
        {
            try
            {
                _rewardedAdService.TryShow(
                    RewardedAdPlacement.BoosterReward,
                    new RewardedAdCallbacks(
                        () => OnBoosterAdRewarded(boosterType),
                        closed: null,
                        displayFailed: null));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnBoosterAdRewarded(BoosterType boosterType)
        {
            try
            {
                _boosterManager.Add(boosterType, amount: 1);
                UpdateUiAfterBoosterGranted(boosterType);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void UpdateUiAfterBoosterGranted(BoosterType boosterType)
        {
            _uiManager.HideBoosterBuyPopup();
            _uiManager.RefreshBoosterInventory();
            _uiManager.RefreshOpenedResourceBars();
            Debug.Log(
                $"Granted 1x {boosterType} booster. " +
                $"Total: {_boosterManager.GetCount(boosterType)}");
        }

        private void OnLeaveGameRequested()
        {
            _playerProfileService.TrySpendHeart();
            BackToHome();
        }

        private bool OnRestartGameRequested()
        {
            if (_isTransitionRunning ||
                _activeLevelNumber <= 0 ||
                !CanLoadLevel(_activeLevelNumber) ||
                !_playerProfileService.TrySpendHeart())
            {
                return false;
            }

            _ = EnterLevelWithLoadingSafelyAsync(_activeLevelNumber);
            return true;
        }

        private void OnGameplayHomeRequested()
        {
            BackToHome();
        }

        private void OnGameplayRetryRequested(int levelNumber)
        {
            StartLevel(levelNumber);
        }

        private void OnGameplayLevelLost(int levelNumber)
        {
            if (levelNumber != _activeLevelNumber)
            {
                return;
            }

            _playerProfileService.TrySpendHeart();
        }

        private void OnGameplayLevelWon(int completedLevelNumber)
        {
            if (completedLevelNumber != _activeLevelNumber)
            {
                return;
            }

            long regularCoinReward = _economyConfig.LevelCompleteCoinReward;
            long doubleCoinReward = checked(
                regularCoinReward * _economyConfig.RewardedAdCoinMultiplier);

            _uiManager.ShowWinPopup(
                OnRegularWinRewardSelected,
                OnRewardedAdWinRewardSelected,
                regularCoinReward,
                doubleCoinReward);
        }

        private void OnRegularWinRewardSelected()
        {
            CompleteWinReward(_economyConfig.LevelCompleteCoinReward);
        }

        private void OnRewardedAdWinRewardSelected()
        {
            try
            {
                _rewardedAdService.TryShow(
                    RewardedAdPlacement.LevelCompleteCoinReward,
                    new RewardedAdCallbacks(
                        OnRewardedAdRewarded,
                        closed: null,
                        displayFailed: null));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnRewardedAdRewarded()
        {
            long coinReward = checked(
                (long)_economyConfig.LevelCompleteCoinReward *
                _economyConfig.RewardedAdCoinMultiplier);
            CompleteWinReward(coinReward);
        }

        private void CompleteWinReward(long coinReward)
        {
            if (!TryBeginTransition())
            {
                return;
            }

            try
            {
                int homeLevelNumber = _activeLevelNumber;
                long startingCoinBalance = _playerProfileService.CoinBalance;

                if (_levelRepository.TryGetNextLevel(_activeLevelNumber, out _))
                {
                    homeLevelNumber++;
                }

                _playerProfileService.ApplyLevelCompletionReward(
                    homeLevelNumber,
                    coinReward);
                HomeCoinRewardPresentation coinRewardPresentation = new(
                    startingCoinBalance,
                    _playerProfileService.CoinBalance,
                    _economyConfig.CoinValuePerRewardImage);
                _ = RunHomeTransitionSafelyAsync(
                    homeLevelNumber,
                    coinRewardPresentation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                FinishTransition();
            }
        }

        private sealed class HomeCoinRewardPresentation
        {
            public HomeCoinRewardPresentation(
                long startingCoinBalance,
                long targetCoinBalance,
                int coinValuePerImage)
            {
                StartingCoinBalance = startingCoinBalance;
                TargetCoinBalance = targetCoinBalance;
                CoinValuePerImage = coinValuePerImage;
            }

            public long StartingCoinBalance { get; }

            public long TargetCoinBalance { get; }

            public int CoinValuePerImage { get; }
        }
    }
}
