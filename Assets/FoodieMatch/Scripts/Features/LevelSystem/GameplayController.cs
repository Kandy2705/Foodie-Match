using System;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
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
        private SelectFoodUseCase _selectFoodUseCase;
        private ILevelRepository _levelRepository;
        private BoardModelFactory _boardModelFactory;
        private Action _homeRequested;

        private BoardModel _board;
        private RequiredPackageModel[] _requiredPackages;
        private WaitingRackModel _waitingRack;
        private int _currentLevelNumber;
        private bool _isInputEnabled;

        public void Construct(
            UIManager uiManager,
            GameplayEvents gameplayEvents,
            BoardLayoutView boardLayoutView,
            RequiredPackageGroupView requiredPackageGroupView,
            WaitingRackView waitingRackView,
            SelectFoodUseCase selectFoodUseCase,
            ILevelRepository levelRepository,
            BoardModelFactory boardModelFactory)
        {
            _uiManager = uiManager;
            _gameplayEvents = gameplayEvents;
            _boardLayoutView = boardLayoutView;
            _requiredPackageGroupView = requiredPackageGroupView;
            _waitingRackView = waitingRackView;
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
            _requiredPackages = _requiredPackageGroupView.CreatePackages();
            _waitingRack = new WaitingRackModel(levelConfig.WaitingRackCapacity);
            _boardLayoutView.Setup(_board);
            _waitingRackView.Clear();
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

            ApplySelectionResult(context, result);
        }

        private void ApplySelectionResult(
            FoodSelectionContext context,
            SelectFoodResult result)
        {
            if (!result.IsPlaced)
            {
                return;
            }

            FoodItemView foodItemView = context.FoodItemView;
            _boardLayoutView.ReleaseFoodItem(foodItemView);

            if (result.Type == SelectFoodResultType.PlacedInRequiredPackage)
            {
                RequiredPackageModel requiredPackage =
                    _requiredPackages[result.TargetIndex];

                _requiredPackageGroupView.ApplyPackageAt(
                    result.TargetIndex,
                    requiredPackage);

                foodItemView.Clear();
                return;
            }

            if (_waitingRackView.SetFoodAt(
                    result.TargetIndex,
                    foodItemView))
            {
                return;
            }

            _waitingRack.TryRemoveFoodAt(
                result.TargetIndex,
                out _);

            if (!_board.TryRestoreFood(
                    context.Address,
                    result.FoodTokenId))
            {
                Debug.LogError("Selected food could not be restored.");
                return;
            }

            _boardLayoutView.RestoreFoodItem(
                foodItemView,
                context.Address);
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
