using System;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using FoodieMatch.UI;
using UnityEngine;

namespace FoodieMatch.Features.LevelSystem
{
    public sealed class GameplayController : MonoBehaviour
    {
        private const string WinReason = "Completed";
        private const string LoseReason = "WaitingRackFull";

        private UIManager _uiManager;
        private GameplayEvents _gameplayEvents;
        private BoardLayoutView _boardLayoutView;
        private RequiredPackageGroupView _requiredPackageGroupView;
        private WaitingRackView _waitingRackView;
        private FoodVisualResolver _foodVisualResolver;
        private RequiredPackageLifecycleUseCase
            _requiredPackageLifecycleUseCase;
        private SelectFoodUseCase _selectFoodUseCase;
        private ILevelRepository _levelRepository;
        private BoardModelFactory _boardModelFactory;
        private Action _homeRequested;

        private BoardModel _board;
        private RequiredPackageModel[] _requiredPackages;
        private RequiredPackageGenerationSettings
            _requiredPackageGenerationSettings;
        private WaitingRackModel _waitingRack;
        private int _currentLevelNumber;
        private bool _isInputEnabled;

        public void Construct(
            UIManager uiManager,
            GameplayEvents gameplayEvents,
            BoardLayoutView boardLayoutView,
            RequiredPackageGroupView requiredPackageGroupView,
            WaitingRackView waitingRackView,
            FoodVisualResolver foodVisualResolver,
            RequiredPackageLifecycleUseCase requiredPackageLifecycleUseCase,
            SelectFoodUseCase selectFoodUseCase,
            ILevelRepository levelRepository,
            BoardModelFactory boardModelFactory)
        {
            _uiManager = uiManager;
            _gameplayEvents = gameplayEvents;
            _boardLayoutView = boardLayoutView;
            _requiredPackageGroupView = requiredPackageGroupView;
            _waitingRackView = waitingRackView;
            _foodVisualResolver = foodVisualResolver;
            _requiredPackageLifecycleUseCase =
                requiredPackageLifecycleUseCase;
            _selectFoodUseCase = selectFoodUseCase;
            _levelRepository = levelRepository;
            _boardModelFactory = boardModelFactory;

            if (_boardLayoutView != null)
            {
                _boardLayoutView.FoodSelected += HandleFoodSelected;
            }
        }

        public void StartLevel(
            int levelNumber,
            Action homeRequested)
        {
            if (!HasDependencies())
            {
                return;
            }

            if (!_levelRepository.TryGetLevel(
                    levelNumber,
                    out LevelConfig levelConfig))
            {
                Debug.LogError($"Level {levelNumber} could not be loaded.");
                return;
            }

            if (_waitingRackView.Capacity != levelConfig.WaitingRackCapacity)
            {
                Debug.LogError(
                    $"Waiting rack capacity must be {levelConfig.WaitingRackCapacity}.");
                return;
            }

            _currentLevelNumber = levelNumber;
            _homeRequested = homeRequested;
            _board = _boardModelFactory.Create(levelConfig);
            _requiredPackageGenerationSettings =
                levelConfig.RequiredPackageGenerationSettings;

            if (!_foodVisualResolver.TryCreateRandomMapping(
                    _board.GetAllFoodTokenIds()))
            {
                Debug.LogError(
                    $"Food visual mapping could not be created " +
                    $"for level {levelNumber}.");

                _board = null;
                return;
            }

            _waitingRack = new WaitingRackModel(levelConfig.WaitingRackCapacity);

            if (_requiredPackageGroupView.PackageCount !=
                _requiredPackageGenerationSettings
                    .InitialActivePackageCount)
            {
                Debug.LogError(
                    "Required package view count does not match the level config.");

                _board = null;
                return;
            }

            if (!_requiredPackageLifecycleUseCase
                    .TryCreateInitialPackages(
                        _board,
                        _waitingRack,
                        _requiredPackageGenerationSettings,
                        out _requiredPackages))
            {
                Debug.LogError(
                    $"Initial required packages could not be created for level {levelNumber}.");

                _board = null;
                _waitingRack = null;
                return;
            }

            _boardLayoutView.Setup(_board);
            _waitingRackView.Clear();
            RefreshRequiredPackageViews();
            _isInputEnabled = true;

            Debug.Log($"Start Level {levelNumber}");

            _gameplayEvents.OnLevelStarted(
                new LevelStartedEvent(levelNumber));
            _gameplayEvents.OnLevelProgressChanged(new LevelProgressChangedEvent(0, 10));
        }

        public void ResolveWin()
        {
            if (!HasDependencies())
            {
                return;
            }

            _isInputEnabled = false;

            _gameplayEvents.OnLevelEnded(
                new LevelEndedEvent(
                    _currentLevelNumber,
                    true,
                    WinReason));

            _uiManager.ShowWinPopup(
                OnNextLevelClicked,
                OnHomeClicked);
        }

        public void ResolveLose()
        {
            if (!HasDependencies())
            {
                return;
            }

            _isInputEnabled = false;

            _gameplayEvents.OnLevelEnded(
                new LevelEndedEvent(
                    _currentLevelNumber,
                    false,
                    LoseReason));
        }

        public void ClearLevel()
        {
            _isInputEnabled = false;

            Debug.Log("Clear Level");
        }

        private bool HasDependencies()
        {
            if (_uiManager == null)
            {
                Debug.LogError("UIManager has not been constructed.");
                return false;
            }

            if (_gameplayEvents == null)
            {
                Debug.LogError("GameplayEvents has not been constructed.");
                return false;
            }

            if (_boardLayoutView == null)
            {
                Debug.LogError("BoardLayoutView has not been constructed.");
                return false;
            }

            if (_requiredPackageGroupView == null)
            {
                Debug.LogError("RequiredPackageGroupView has not been constructed.");
                return false;
            }

            if (_waitingRackView == null)
            {
                Debug.LogError("WaitingRackView has not been constructed.");
                return false;
            }

            if (_foodVisualResolver == null)
            {
                Debug.LogError("FoodVisualResolver has not been constructed.");
                return false;
            }

            if (_requiredPackageLifecycleUseCase == null)
            {
                Debug.LogError(
                    "RequiredPackageLifecycleUseCase has not been constructed.");
                return false;
            }

            if (_selectFoodUseCase == null)
            {
                Debug.LogError("SelectFoodUseCase has not been constructed.");
                return false;
            }

            if (_levelRepository == null)
            {
                Debug.LogError("LevelRepository has not been constructed.");
                return false;
            }

            if (_boardModelFactory == null)
            {
                Debug.LogError("BoardModelFactory has not been constructed.");
                return false;
            }

            return true;
        }

        private void OnDestroy()
        {
            if (_boardLayoutView != null)
            {
                _boardLayoutView.FoodSelected -= HandleFoodSelected;
            }
        }

        private void HandleFoodSelected(FoodSelectionContext context)
        {
            if (!_isInputEnabled ||
                context.FoodItemView == null ||
                _requiredPackages == null ||
                _waitingRack == null)
            {
                return;
            }

            SelectFoodResult result = _selectFoodUseCase.Execute(
                context.Address,
                _board,
                _requiredPackages,
                _waitingRack);

            if (!ApplySelectionResult(context, result))
            {
                return;
            }

            MoveTopTrayToGrill(
                context.Address.GrillPositionIndex);

            ResolveRequiredPackageLifecycle(result);
        }

        private bool ApplySelectionResult(
            FoodSelectionContext context,
            SelectFoodResult result)
        {
            if (!result.IsPlaced)
            {
                return false;
            }

            FoodItemView foodItemView = context.FoodItemView;
            _boardLayoutView.ReleaseFoodItem(foodItemView);

            if (result.Type == SelectFoodResultType.PlacedInRequiredPackage)
            {
                RequiredPackageModel requiredPackage =
                    _requiredPackages[result.TargetIndex];

                foodItemView.Clear();

                if (!requiredPackage.IsComplete)
                {
                    _requiredPackageGroupView.UpdateFilledAmountAt(
                        result.TargetIndex,
                        requiredPackage);
                }

                return true;
            }

            if (_waitingRackView.SetFoodAt(
                    result.TargetIndex,
                    foodItemView))
            {
                return true;
            }

            _waitingRack.TryRemoveFoodAt(
                result.TargetIndex,
                out _);

            if (!_board.TryRestoreFood(
                    context.Address,
                    result.FoodTokenId))
            {
                Debug.LogError("Selected food could not be restored.");
                return false;
            }

            _boardLayoutView.RestoreFoodItem(
                foodItemView,
                context.Address);

            return false;
        }

        private void ResolveRequiredPackageLifecycle(
            SelectFoodResult result)
        {
            if (result.Type !=
                SelectFoodResultType.PlacedInRequiredPackage)
            {
                return;
            }

            RequiredPackageModel requiredPackage =
                _requiredPackages[result.TargetIndex];

            if (!requiredPackage.IsComplete)
            {
                return;
            }

            RequiredPackageLifecycleResult lifecycleResult =
                _requiredPackageLifecycleUseCase
                    .ResolveCompletedPackage(
                        result.TargetIndex,
                        _board,
                        _waitingRack,
                        _requiredPackages,
                        _requiredPackageGenerationSettings);

            ApplyLifecycleResult(lifecycleResult);
        }

        private void ApplyLifecycleResult(
            RequiredPackageLifecycleResult lifecycleResult)
        {
            for (int i = 0;
                 i < lifecycleResult.Transfers.Count;
                 i++)
            {
                WaitingRackTransfer transfer =
                    lifecycleResult.Transfers[i];
                FoodItemView foodItemView =
                    _waitingRackView.RemoveFoodAt(
                        transfer.RackSlotIndex);

                if (foodItemView != null)
                {
                    foodItemView.Clear();
                }
            }

            for (int i = 0;
                 i < lifecycleResult.UpdatedPackageIndexes.Count;
                 i++)
            {
                RefreshRequiredPackageViewAt(
                    lifecycleResult.UpdatedPackageIndexes[i]);
            }
        }

        private void RefreshRequiredPackageViews()
        {
            for (int i = 0; i < _requiredPackages.Length; i++)
            {
                RefreshRequiredPackageViewAt(i);
            }
        }

        private void RefreshRequiredPackageViewAt(int packageIndex)
        {
            RequiredPackageModel package =
                _requiredPackages[packageIndex];
            Sprite sprite = package != null
                ? _foodVisualResolver.ResolveIcon(
                    package.FoodTokenId)
                : null;

            if (!_requiredPackageGroupView.ShowPackageAt(
                    packageIndex,
                    package,
                    sprite))
            {
                Debug.LogError(
                    $"Required package view {packageIndex} could not be updated.");
            }
        }

        private void MoveTopTrayToGrill(
            int grillPositionIndex)
        {
            if (!_board.TryMoveTopTrayToGrill(
                    grillPositionIndex,
                    out GrillModel grillModel))
            {
                return;
            }

            if (!_boardLayoutView.MoveTopTrayFoodToGrill(
                    grillModel))
            {
                Debug.LogError(
                    $"Could not move top tray to grill {grillPositionIndex}.");
            }
        }

        private void OnNextLevelClicked()
        {
            if (!_levelRepository.TryGetNextLevel(
                    _currentLevelNumber,
                    out _))
            {
                Debug.Log("No next level is available.");
                return;
            }

            _uiManager.HideAllPopups();
            StartLevel(_currentLevelNumber + 1, _homeRequested);
        }

        private void OnHomeClicked()
        {
            _uiManager.HideAllPopups();
            ClearLevel();

            _homeRequested?.Invoke();
        }
    }
}
