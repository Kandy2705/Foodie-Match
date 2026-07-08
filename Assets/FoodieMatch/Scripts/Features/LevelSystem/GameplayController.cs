using System;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Features.Board;
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
        private Action _homeRequested;

        private int _currentLevelId;
        private bool _isInputEnabled;

        public void Construct(
            UIManager uiManager,
            GameplayEvents gameplayEvents,
            BoardLayoutView boardLayoutView)
        {
            _uiManager = uiManager;
            _gameplayEvents = gameplayEvents;
            _boardLayoutView = boardLayoutView;

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
            _isInputEnabled = true;

            Debug.Log($"Start Level {levelId}");

            _gameplayEvents.OnLevelStarted(new LevelStartedEvent(levelId));
            _gameplayEvents.OnLevelProgressChanged(new LevelProgressChangedEvent(0, 10));
        }

        public void SelectFood(int foodId)
        {
            if (!_isInputEnabled)
            {
                return;
            }

            Debug.Log($"Select Food {foodId}");
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
            SelectFood(context.FoodTokenId);
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
