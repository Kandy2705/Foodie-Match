using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.GameState;
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

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayController : MonoBehaviour
    {
        private const string WinReason = "Completed";
        private const string LoseReason = "WaitingRackFull";

        private readonly GameplaySessionGuard _sessionGuard = new();

        private UIManager _uiManager;
        private GameplayEvents _gameplayEvents;
        private BoardLayoutView _boardLayoutView;
        private RequiredPackageGroupView _requiredPackageGroupView;
        private WaitingRackView _waitingRackView;
        private GameplayMotionPresenter _gameplayMotionPresenter;
        private FoodVisualResolver _foodVisualResolver;
        private RequiredPackageLifecycleUseCase _requiredPackageLifecycleUseCase;
        private SelectFoodUseCase _selectFoodUseCase;
        private ILevelRepository _levelRepository;
        private BoardModelFactory _boardModelFactory;
        private PackageDeliveryCoordinator _packageDeliveryCoordinator;
        private WaitingRackPlacementCoordinator _waitingRackPlacementCoordinator;
        private WaitingRackAutoFillCoordinator _waitingRackAutoFillCoordinator;
        private TopTrayMoveCoordinator _topTrayMoveCoordinator;
        private GameplaySession _session;
        private Action _homeRequested;

        private void OnDestroy()
        {
            _sessionGuard.EndSession();
            _gameplayMotionPresenter?.CancelAllMotions();
            _packageDeliveryCoordinator?.EndSession();
            _waitingRackAutoFillCoordinator?.EndSession();

            if (_boardLayoutView != null)
            {
                _boardLayoutView.FoodSelected -= HandleFoodSelected;
            }

            UnsubscribeCoordinatorEvents();
        }

        public void Construct(
            UIManager uiManager,
            GameplayEvents gameplayEvents,
            BoardLayoutView boardLayoutView,
            RequiredPackageGroupView requiredPackageGroupView,
            WaitingRackView waitingRackView,
            GameplayMotionPresenter gameplayMotionPresenter,
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
            _gameplayMotionPresenter = gameplayMotionPresenter;
            _foodVisualResolver = foodVisualResolver;
            _requiredPackageLifecycleUseCase = requiredPackageLifecycleUseCase;
            _selectFoodUseCase = selectFoodUseCase;
            _levelRepository = levelRepository;
            _boardModelFactory = boardModelFactory;

            CreateCoordinators();
            SubscribeCoordinatorEvents();

            if (_boardLayoutView != null)
            {
                _boardLayoutView.FoodSelected += HandleFoodSelected;
            }
        }

        public void StartLevel(int levelNumber, Action homeRequested)
        {
            if (!HasDependencies())
            {
                return;
            }

            if (!_levelRepository.TryGetLevel(levelNumber, out LevelConfig levelConfig))
            {
                Debug.LogError($"Level {levelNumber} could not be loaded.");
                return;
            }

            if (_waitingRackView.Capacity != levelConfig.WaitingRackCapacity)
            {
                Debug.LogError($"Waiting rack capacity must be {levelConfig.WaitingRackCapacity}.");
                return;
            }

            _homeRequested = homeRequested;
            BoardModel board = _boardModelFactory.Create(levelConfig);
            RequiredPackageGenerationSettings packageSettings = levelConfig.RequiredPackageGenerationSettings;

            if (!_foodVisualResolver.TryCreateRandomMapping(board.GetAllFoodTokenIds()))
            {
                Debug.LogError($"Food visual mapping could not be created for level {levelNumber}.");
                return;
            }

            WaitingRackModel waitingRack = new(levelConfig.WaitingRackCapacity);

            if (_requiredPackageGroupView.PackageCount != packageSettings.InitialActivePackageCount)
            {
                Debug.LogError("Required package view count does not match the level config.");
                return;
            }

            if (!_requiredPackageLifecycleUseCase.TryCreateInitialPackages(
                    board, waitingRack, packageSettings, out RequiredPackageModel[] requiredPackages))
            {
                Debug.LogError($"Initial required packages could not be created for level {levelNumber}.");
                return;
            }

            int sessionId = _sessionGuard.BeginSession();
            LevelProgressModel progress = new(board.RemainingFoodCount);
            _session = new(
                sessionId,
                levelNumber,
                board,
                requiredPackages,
                waitingRack,
                progress,
                packageSettings);

            _gameplayMotionPresenter.CancelAllMotions();
            _boardLayoutView.Setup(_session.Board);
            _waitingRackView.Clear();
            _packageDeliveryCoordinator.BeginSession(_session);
            _waitingRackAutoFillCoordinator.BeginSession(_session);
            _session.StartPlaying();

            Debug.Log($"Start Level {levelNumber}");
            _gameplayEvents.OnLevelStarted(new LevelStartedEvent(levelNumber));
            _gameplayEvents.OnLevelProgressChanged(
                new LevelProgressChangedEvent(_session.Progress.ServedCount, _session.Progress.TotalCount));
        }

        public void ClearLevel()
        {
            _sessionGuard.EndSession();
            _gameplayMotionPresenter?.CancelAllMotions();
            _packageDeliveryCoordinator?.EndSession();
            _waitingRackAutoFillCoordinator?.EndSession();
            _session = null;

            Debug.Log("Clear Level");
        }

        private void CreateCoordinators()
        {
            _packageDeliveryCoordinator = new(
                _sessionGuard, _gameplayMotionPresenter, _requiredPackageLifecycleUseCase,
                _requiredPackageGroupView, _foodVisualResolver, _gameplayEvents);
            _waitingRackPlacementCoordinator = new(_sessionGuard, _gameplayMotionPresenter, _waitingRackView);
            _waitingRackAutoFillCoordinator = new(
                _sessionGuard, _requiredPackageLifecycleUseCase, _waitingRackView, _packageDeliveryCoordinator);
            _topTrayMoveCoordinator = new(_sessionGuard, _gameplayMotionPresenter, _boardLayoutView);
        }

        private void SubscribeCoordinatorEvents()
        {
            _packageDeliveryCoordinator.PackageReplaced += HandlePackageReplaced;
            _packageDeliveryCoordinator.PackageDeliveryFailed += HandleGameplayFlowFailed;
            _waitingRackAutoFillCoordinator.AutoFillFinished += HandleAutoFillFinished;
            _waitingRackAutoFillCoordinator.AutoFillFailed += HandleGameplayFlowFailed;
        }

        private void UnsubscribeCoordinatorEvents()
        {
            if (_packageDeliveryCoordinator != null)
            {
                _packageDeliveryCoordinator.PackageReplaced -= HandlePackageReplaced;
                _packageDeliveryCoordinator.PackageDeliveryFailed -= HandleGameplayFlowFailed;
            }

            if (_waitingRackAutoFillCoordinator != null)
            {
                _waitingRackAutoFillCoordinator.AutoFillFinished -= HandleAutoFillFinished;
                _waitingRackAutoFillCoordinator.AutoFillFailed -= HandleGameplayFlowFailed;
            }
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

            if (_gameplayMotionPresenter == null)
            {
                Debug.LogError("GameplayMotionPresenter has not been constructed.");
                return false;
            }

            if (_foodVisualResolver == null)
            {
                Debug.LogError("FoodVisualResolver has not been constructed.");
                return false;
            }

            if (_requiredPackageLifecycleUseCase == null)
            {
                Debug.LogError("RequiredPackageLifecycleUseCase has not been constructed.");
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

        private void HandleFoodSelected(FoodSelectionContext context)
        {
            _ = ProcessFoodSelectionSafelyAsync(context);
        }

        private async Task ProcessFoodSelectionSafelyAsync(FoodSelectionContext context)
        {
            try
            {
                await ProcessFoodSelectionAsync(context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async Task ProcessFoodSelectionAsync(FoodSelectionContext context)
        {
            GameplaySession session = _session;

            if (session == null || !session.CanSelectFood || context.FoodItemView == null)
            {
                return;
            }

            SelectFoodResult result = _selectFoodUseCase.Execute(
                context.Address, session.Board, session.RequiredPackages, session.WaitingRack);

            if (!result.IsPlaced)
            {
                return;
            }

            if (result.Type == SelectFoodResultType.PlacedInRequiredPackage)
            {
                await ProcessRequiredPackageSelectionAsync(context, result, session);
                return;
            }

            await ProcessWaitingRackSelectionAsync(context, result, session);
        }

        private async Task ProcessRequiredPackageSelectionAsync(
            FoodSelectionContext context,
            SelectFoodResult result,
            GameplaySession session)
        {
            _boardLayoutView.ReleaseFoodItem(context.FoodItemView);
            Task deliveryTask = _packageDeliveryCoordinator.DeliverSelectedFoodAsync(
                context.FoodItemView, result.TargetIndex, session);

            _topTrayMoveCoordinator.MoveFoodToGrill(context.Address.GrillPositionIndex, session);
            await deliveryTask;
            TryResolveWin(session);
        }

        private async Task ProcessWaitingRackSelectionAsync(
            FoodSelectionContext context,
            SelectFoodResult result,
            GameplaySession session)
        {
            _boardLayoutView.ReleaseFoodItem(context.FoodItemView);
            Task<WaitingRackPlacementResult> placementTask = _waitingRackPlacementCoordinator.PlaceFoodAsync(
                context.FoodItemView, result.TargetIndex, session);

            _topTrayMoveCoordinator.MoveFoodToGrill(context.Address.GrillPositionIndex, session);
            bool causedWaitingRackFull = session.WaitingRack.IsFull;

            if (causedWaitingRackFull)
            {
                session.TryEnterAwaitingRevive();
            }

            WaitingRackPlacementResult placementResult = await placementTask;

            if (!IsCurrentSession(session) || placementResult == WaitingRackPlacementResult.Cancelled)
            {
                return;
            }

            if (placementResult == WaitingRackPlacementResult.Failed)
            {
                session.DisableInput();
                return;
            }

            if (session.CanContinueGameplay)
            {
                _waitingRackAutoFillCoordinator.StartOrRequestRetry(session);
            }

            if (causedWaitingRackFull && session.State == LevelSessionState.AwaitingRevive)
            {
                ShowLosePopup(session);
            }
        }

        private void HandlePackageReplaced(GameplaySession session)
        {
            if (!IsCurrentSession(session))
            {
                return;
            }

            if (session.CanContinueGameplay)
            {
                _waitingRackAutoFillCoordinator.StartOrRequestRetry(session);
            }

            TryResolveWin(session);
        }

        private void HandleAutoFillFinished(GameplaySession session)
        {
            TryResolveWin(session);
        }

        private void HandleGameplayFlowFailed(GameplaySession session)
        {
            if (IsCurrentSession(session))
            {
                session.DisableInput();
            }
        }

        private void TryResolveWin(GameplaySession session)
        {
            if (!IsCurrentSession(session) ||
                !session.CanContinueGameplay ||
                !session.Progress.IsComplete ||
                !session.IsDisplayedProgressUpToDate ||
                _waitingRackAutoFillCoordinator.IsRunning(session) ||
                _packageDeliveryCoordinator.HasActiveMotion(session))
            {
                return;
            }

            ResolveWin(session);
        }

        private void ResolveWin(GameplaySession session)
        {
            if (!HasDependencies() || !IsCurrentSession(session) || !session.TryMarkAsWon())
            {
                return;
            }

            _gameplayEvents.OnLevelEnded(new LevelEndedEvent(session.LevelNumber, true, WinReason));
            _uiManager.ShowWinPopup(OnNextLevelClicked, OnHomeClicked);
        }

        private void ShowLosePopup(GameplaySession session)
        {
            if (!IsCurrentSession(session) || session.State != LevelSessionState.AwaitingRevive)
            {
                return;
            }

            _uiManager.ShowLosePopup(OnTryAgainClicked, OnHomeClicked);
        }

        private void FinalizeLose()
        {
            GameplaySession session = _session;

            if (session == null || !session.TryMarkAsLost())
            {
                return;
            }

            _gameplayEvents.OnLevelEnded(new LevelEndedEvent(session.LevelNumber, false, LoseReason));
        }

        private bool IsCurrentSession(GameplaySession session)
        {
            return session != null && _session == session && _sessionGuard.IsCurrentSession(session.SessionId);
        }

        private void OnNextLevelClicked()
        {
            GameplaySession session = _session;

            if (session == null || !_levelRepository.TryGetNextLevel(session.LevelNumber, out _))
            {
                Debug.Log("No next level is available.");
                return;
            }

            _uiManager.HideAllPopups();
            StartLevel(session.LevelNumber + 1, _homeRequested);
        }

        private void OnTryAgainClicked()
        {
            if (_session == null)
            {
                return;
            }

            int levelNumber = _session.LevelNumber;
            FinalizeLose();
            _uiManager.HideAllPopups();
            StartLevel(levelNumber, _homeRequested);
        }

        private void OnHomeClicked()
        {
            FinalizeLose();
            _uiManager.HideAllPopups();
            ClearLevel();
            _homeRequested?.Invoke();
        }
    }
}
