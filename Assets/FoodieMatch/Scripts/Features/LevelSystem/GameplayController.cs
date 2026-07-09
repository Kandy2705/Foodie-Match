using System;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.WaitingRack;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using FoodieMatch.UI;
using UnityEngine;
using RequiredPackageDomain = FoodieMatch.Core.Domain.RequiredPackage.RequiredPackage;

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
        private Action _homeRequested;

        private RequiredPackageDomain[] _requiredPackages;
        private WaitingRackState _waitingRackState;
        private int _currentLevelId;
        private bool _isInputEnabled;

        public void Construct(
            UIManager uiManager,
            GameplayEvents gameplayEvents,
            BoardLayoutView boardLayoutView,
            RequiredPackageGroupView requiredPackageGroupView,
            WaitingRackView waitingRackView,
            SelectFoodUseCase selectFoodUseCase)
        {
            _uiManager = uiManager;
            _gameplayEvents = gameplayEvents;
            _boardLayoutView = boardLayoutView;
            _requiredPackageGroupView = requiredPackageGroupView;
            _waitingRackView = waitingRackView;
            _selectFoodUseCase = selectFoodUseCase;

            if (_boardLayoutView != null)
            {
                _boardLayoutView.FoodSelected += HandleFoodSelected;
            }
        }

        public void StartLevel(
            int levelId,
            Action homeRequested)
        {
            if (!HasDependencies())
            {
                return;
            }

            _currentLevelId = levelId;
            _homeRequested = homeRequested;
            _requiredPackages = _requiredPackageGroupView.CreatePackages();
            _waitingRackState = new WaitingRackState(_waitingRackView.Capacity);
            _waitingRackView.Clear();
            _isInputEnabled = true;

            Debug.Log($"Start Level {levelId}");

            _gameplayEvents.OnLevelStarted(new LevelStartedEvent(levelId));
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
                    _currentLevelId,
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
                    _currentLevelId,
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
                _waitingRackState == null)
            {
                return;
            }

            SelectFoodResult result = _selectFoodUseCase.Execute(
                context.FoodTokenId,
                _requiredPackages,
                _waitingRackState);

            ApplySelectionResult(context.FoodItemView, result);
        }

        private void ApplySelectionResult(
            FoodItemView foodItemView,
            SelectFoodResult result)
        {
            if (!result.IsPlaced)
            {
                return;
            }

            _boardLayoutView.ReleaseFoodItem(foodItemView);

            if (result.Type == SelectFoodResultType.PlacedInRequiredPackage)
            {
                RequiredPackageDomain requiredPackage =
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

            _waitingRackState.TryRemoveFoodAt(
                result.TargetIndex,
                out _);

            _boardLayoutView.RestoreFoodItem(foodItemView);
        }

        private void OnNextLevelClicked()
        {
            _uiManager.HideAllPopups();
            StartLevel(_currentLevelId + 1, _homeRequested);
        }

        private void OnHomeClicked()
        {
            _uiManager.HideAllPopups();
            ClearLevel();

            _homeRequested?.Invoke();
        }
    }
}
