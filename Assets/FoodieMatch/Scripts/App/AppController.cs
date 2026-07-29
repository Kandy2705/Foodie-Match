using System;
using System.Threading.Tasks;
using FoodieMatch.App.Advertising;
using FoodieMatch.Core.Application.Advertising;
using FoodieMatch.Core.Application.Audio;
using FoodieMatch.Core.Application.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.Level;
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
        private ShopPurchaseService _shopPurchaseService;
        private IRewardedAdService _rewardedAdService;
        private PostLevelAdCoordinator _postLevelAdCoordinator;
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
            ShopPurchaseService shopPurchaseService,
            IRewardedAdService rewardedAdService,
            PostLevelAdCoordinator postLevelAdCoordinator,
            ILevelRepository levelRepository,
            IAudioService audioService)
        {
            _uiManager = uiManager;
            _gameplayController = gameplayController;
            _playerProfileService = playerProfileService;
            _boosterManager = boosterManager;
            _economyConfig = economyConfig;
            _shopPurchaseService = shopPurchaseService;
            _rewardedAdService = rewardedAdService;
            _postLevelAdCoordinator = postLevelAdCoordinator;
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
            _uiManager.AddBoxCoinPaymentRequested += OnAddBoxCoinPaymentRequested;
            _uiManager.AddBoxRewardedAdRequested += OnAddBoxRewardedAdRequested;
            _uiManager.FillHeartCoinPurchaseRequested +=
                OnFillHeartCoinPurchaseRequested;
            _uiManager.FillHeartRewardedAdRequested +=
                OnFillHeartRewardedAdRequested;
            _uiManager.BoosterUseHandler = OnBoosterUseRequested;
            _uiManager.RestartGameHandler = OnRestartGameRequested;
            _uiManager.ShopPurchaseHandler = OnShopPurchaseRequestedAsync;
        }

        public void EnterHome()
        {
            _ = EnterHomeAsync();
        }

        public Task EnterHomeAsync()
        {
            int levelNumber = GetSavedPlayableLevelNumber();
            return OpenHomeAsync(
                levelNumber,
                _playerProfileService.CoinBalance);
        }

        public void StartLevel(int levelNumber)
        {
            if (!CanLoadLevel(levelNumber))
            {
                return;
            }

            if (!_playerProfileService.HasAvailableHeart())
            {
                _uiManager.ShowFillHeartPopup(
                    () => StartLevel(levelNumber));
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
                await loadingTask;

                if (!_levelRepository.TryGetLevel(
                        levelNumber,
                        out LevelDefinition levelDefinition))
                {
                    throw new InvalidOperationException(
                        $"Level {levelNumber} disappeared during transition.");
                }

                await OpenLevelAsync(
                    levelNumber,
                    enableGameplayInput: false);

                try
                {
                    await _uiManager.PlayLevelWarningAsync(
                        levelDefinition.Difficulty);
                }
                finally
                {
                    _gameplayController.EnableGameplayInput();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                await FinishTransitionAsync();
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
                await OpenHomeAsync(levelNumber, displayedCoinBalance);
                shouldPlayCoinReward = coinRewardPresentation != null;

                await loadingTask;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                await FinishTransitionAsync();
            }

            if (shouldPlayCoinReward)
            {
                _uiManager.PlayHomeCoinReward(
                    coinRewardPresentation.StartingCoinBalance,
                    coinRewardPresentation.TargetCoinBalance,
                    coinRewardPresentation.CoinValuePerImage);
            }
        }

        private async Task OpenLevelAsync(
            int levelNumber,
            bool enableGameplayInput)
        {
            _gameplayController.ClearLevel();
            _uiManager.HideAllPopups();
            _uiManager.HideHome();
            _uiManager.SetCurrentLevelNumber(levelNumber);
            await _uiManager.ShowGameplayHudAsync();
            _gameplayController.SetPresentationActive(true);
            _audioService.PlayMusic(AudioKeys.MusicIngame);

            _playerProfileService.SetCurrentLevelNumber(levelNumber);
            _activeLevelNumber = levelNumber;
            _gameplayController.StartLevel(
                levelNumber,
                _gameplayNavigationActions,
                enableGameplayInput);
        }

        private async Task OpenHomeAsync(
            int levelNumber,
            long displayedCoinBalance)
        {
            _gameplayController.ClearLevel();
            _gameplayController.SetPresentationActive(false);
            _uiManager.HideAllPopups();
            _uiManager.HideGameplayHud();
            _uiManager.SetCurrentLevelNumber(levelNumber);
            await _uiManager.ShowHomeAsync(displayedCoinBalance);
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

        private async Task FinishTransitionAsync()
        {
            await _uiManager.HideLoadingAsync();
            _isTransitionRunning = false;
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
            TryShowRewardedAd(
                GetBoosterAdPlacement(boosterType),
                () => OnBoosterAdRewarded(boosterType));
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

        private void OnAddBoxCoinPaymentRequested()
        {
            try
            {
                int coinPrice = _economyConfig.GetBoosterPrice(BoosterType.Box);

                if (!_playerProfileService.TrySpendCoins(coinPrice))
                {
                    return;
                }

                _uiManager.RefreshOpenedResourceBars();
                _uiManager.CompleteAddBoxRequest();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnAddBoxRewardedAdRequested()
        {
            TryShowRewardedAd(
                GetBoosterAdPlacement(BoosterType.Box),
                OnAddBoxAdRewarded);
        }

        private void OnAddBoxAdRewarded()
        {
            _uiManager.CompleteAddBoxRequest();
        }

        private void OnFillHeartCoinPurchaseRequested()
        {
            int coinPrice = _economyConfig.FullHeartCoinPrice;

            if (!_playerProfileService.TryFillHeartsWithCoins(coinPrice))
            {
                _uiManager.ShowActionFeedback("Not enough coins.");
                return;
            }

            _uiManager.CompleteHeartRefill();
        }

        private void OnFillHeartRewardedAdRequested()
        {
            TryShowRewardedAd(
                RewardedAdPlacement.AddHeart,
                OnFillHeartAdRewarded);
        }

        private void OnFillHeartAdRewarded()
        {
            if (_playerProfileService.TryAddHeart())
            {
                _uiManager.CompleteHeartRefill();
            }
        }

        private void UpdateUiAfterBoosterGranted(BoosterType boosterType)
        {
            _uiManager.HideBoosterBuyPopup();
            _uiManager.RefreshAllPlayerResources();
            Debug.Log(
                $"Granted 1x {boosterType} booster. " +
                $"Total: {_boosterManager.GetCount(boosterType)}");
        }

        private async Task<ShopPurchaseResult> OnShopPurchaseRequestedAsync(
            string productId)
        {
            ShopPurchaseResult result =
                await _shopPurchaseService.PurchaseAsync(productId);

            if (result.IsSuccess)
            {
                _uiManager.RefreshAllPlayerResources();
                _uiManager.ShowActionFeedback("Purchase successful!");
            }
            else
            {
                Debug.LogError(
                    $"Shop purchase {productId} failed: {result.ErrorMessage}");
            }

            return result;
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
                !CanLoadLevel(_activeLevelNumber))
            {
                return false;
            }

            if (!_playerProfileService.TrySpendHeart())
            {
                _uiManager.ShowFillHeartPopup(RestartAfterHeartRefill);
                return false;
            }

            _ = EnterLevelWithLoadingSafelyAsync(_activeLevelNumber);
            return true;
        }

        private void RestartAfterHeartRefill()
        {
            if (OnRestartGameRequested())
            {
                _uiManager.HideAllPopups();
            }
        }

        private void OnGameplayHomeRequested()
        {
            _postLevelAdCoordinator.RunAfterPostLevelAd(BackToHome);
        }

        private void OnGameplayRetryRequested(int levelNumber)
        {
            _postLevelAdCoordinator.RunAfterPostLevelAd(
                () => StartLevel(levelNumber));
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
            _postLevelAdCoordinator.RunAfterPostLevelAd(
                () => CompleteWinReward(
                    _economyConfig.LevelCompleteCoinReward));
        }

        private void OnRewardedAdWinRewardSelected()
        {
            TryShowRewardedAd(
                RewardedAdPlacement.DoubleCoin,
                OnRewardedAdRewarded);
        }

        private void TryShowRewardedAd(
            RewardedAdPlacement placement,
            Action rewarded)
        {
            RewardedAdCallbacks callbacks = new(
                rewarded,
                displayed: _postLevelAdCoordinator.RecordAdDisplayed,
                closed: null,
                displayFailed: ShowAdNotReadyFeedback);

            if (!_rewardedAdService.TryShow(placement, callbacks))
            {
                ShowAdNotReadyFeedback();
            }
        }

        private void ShowAdNotReadyFeedback()
        {
            _uiManager.ShowActionFeedback(
                "Ad is not ready. Please try again.");
        }

        private static RewardedAdPlacement GetBoosterAdPlacement(
            BoosterType boosterType)
        {
            return boosterType switch
            {
                BoosterType.Plate => RewardedAdPlacement.BoosterPlate,
                BoosterType.Storage => RewardedAdPlacement.BoosterStorage,
                BoosterType.Swap => RewardedAdPlacement.BoosterSwap,
                BoosterType.Fridge => RewardedAdPlacement.BoosterFridge,
                BoosterType.Box => RewardedAdPlacement.BoosterBox,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(boosterType),
                    boosterType,
                    null)
            };
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
                _ = FinishTransitionAsync();
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
