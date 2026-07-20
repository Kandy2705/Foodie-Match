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
using FoodieMatch.Data.Booster;
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

        [Header("Combo")]
        [SerializeField] private float _comboDurationSeconds = 8f;

        private readonly GameplaySessionGuard _sessionGuard = new();

        private UIManager _uiManager;
        private GameplayEvents _gameplayEvents;
        private BoardLayoutView _boardLayoutView;
        private RequiredPackageGroupView _requiredPackageGroupView;
        private WaitingRackView _waitingRackView;
        private GameplayMotionPresenter _gameplayMotionPresenter;
        private GameplayAudioPresenter _gameplayAudioPresenter;
        private GameplayWorldClickSfx _gameplayWorldClickSfx;
        private FoodVisualResolver _foodVisualResolver;
        private RequiredPackageLifecycleUseCase _requiredPackageLifecycleUseCase;
        private SelectFoodUseCase _selectFoodUseCase;
        private ILevelRepository _levelRepository;
        private BoardModelFactory _boardModelFactory;
        private PackageDeliveryCoordinator _packageDeliveryCoordinator;
        private WaitingRackPlacementCoordinator _waitingRackPlacementCoordinator;
        private WaitingRackAutoFillCoordinator _waitingRackAutoFillCoordinator;
        private TopTrayMoveCoordinator _topTrayMoveCoordinator;
        private GrillCompletionCoordinator _grillCompletionCoordinator;
        private ComboCoordinator _comboCoordinator;
        private PlateBoosterCoordinator _plateBoosterCoordinator;
        private StorageBoosterCoordinator _storageBoosterCoordinator;
        private GameplaySession _session;
        private GameplayNavigationActions _navigationActions;

        private void Update()
        {
            _comboCoordinator?.AdvanceTime(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _gameplayWorldClickSfx?.StopListening();
            _sessionGuard.EndSession();
            _gameplayMotionPresenter?.CancelAllMotions();
            _packageDeliveryCoordinator?.EndSession();
            _waitingRackAutoFillCoordinator?.EndSession();
            _grillCompletionCoordinator?.EndSession();
            _comboCoordinator?.EndSession();

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
            GameplayAudioPresenter gameplayAudioPresenter,
            GameplayWorldClickSfx gameplayWorldClickSfx,
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
            _gameplayAudioPresenter = gameplayAudioPresenter;
            _gameplayWorldClickSfx = gameplayWorldClickSfx;
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

        public void StartLevel(int levelNumber, GameplayNavigationActions navigationActions)
        {
            if (!HasDependencies())
            {
                return;
            }

            if (!IsValidComboDuration())
            {
                Debug.LogError("Combo duration must be greater than zero.");
                return;
            }

            if (!_levelRepository.TryGetLevel(levelNumber, out LevelConfig levelConfig))
            {
                Debug.LogError($"Level {levelNumber} could not be loaded.");
                return;
            }

            _waitingRackView.ResetToCapacity(WaitingRackRules.InitialCapacity);

            if (_waitingRackView.Capacity != WaitingRackRules.InitialCapacity)
            {
                Debug.LogError($"Waiting rack capacity must be {WaitingRackRules.InitialCapacity}.");
                return;
            }

            _navigationActions = navigationActions ?? throw new ArgumentNullException(nameof(navigationActions));
            BoardModel board = _boardModelFactory.Create(levelConfig);
            RequiredPackageGenerationSettings packageSettings = levelConfig.RequiredPackageGenerationSettings;

            if (!_foodVisualResolver.TryCreateMapping(board.GetAllFoodTokenIds(), levelNumber))
            {
                Debug.LogError($"Food visual mapping could not be created for level {levelNumber}.");
                return;
            }

            WaitingRackModel waitingRack = new(WaitingRackRules.InitialCapacity);

            if (_requiredPackageGroupView.PackageCount != LevelRules.ActivePackageCount)
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
            ComboProgressModel combo = new(_comboDurationSeconds);
            _session = new(
                sessionId,
                levelNumber,
                board,
                requiredPackages,
                waitingRack,
                progress,
                combo,
                packageSettings);

            _gameplayMotionPresenter.CancelAllMotions();
            _boardLayoutView.Setup(_session.Board);
            _waitingRackView.Clear();
            _packageDeliveryCoordinator.BeginSession(_session);
            _waitingRackAutoFillCoordinator.BeginSession(_session);
            _grillCompletionCoordinator.BeginSession(_session);
            _session.StartPlaying();
            _gameplayWorldClickSfx.StartListening();

            Debug.Log($"Start Level {levelNumber}");
            _gameplayEvents.OnLevelStarted(new LevelStartedEvent(levelNumber));
            _comboCoordinator.BeginSession(_session);
            _gameplayEvents.OnLevelProgressChanged(
                new LevelProgressChangedEvent(_session.Progress.ServedCount, _session.Progress.TotalCount));
        }

        public void ClearLevel()
        {
            _gameplayWorldClickSfx?.StopListening();
            _sessionGuard.EndSession();
            _gameplayMotionPresenter?.CancelAllMotions();
            _packageDeliveryCoordinator?.EndSession();
            _waitingRackAutoFillCoordinator?.EndSession();
            _grillCompletionCoordinator?.EndSession();
            _comboCoordinator?.EndSession();
            _waitingRackView?.Clear();
            _session = null;

            Debug.Log("Clear Level");
        }

        public bool TryApplyBooster(BoosterType boosterType)
        {
            if (_session == null ||
                !_session.CanContinueGameplay ||
                !_session.IsInputEnabled)
            {
                return false;
            }

            switch (boosterType)
            {
                case BoosterType.Plate:
                    return TryApplyPlateBooster();
                case BoosterType.Storage:
                    return TryApplyStorageBooster();
                default:
                    Debug.Log($"Booster {boosterType} is not implemented yet.");
                    return false;
            }
        }

        private bool TryApplyPlateBooster()
        {
            return _plateBoosterCoordinator != null &&
                   _plateBoosterCoordinator.TryApply(_session);
        }

        private bool TryApplyStorageBooster()
        {
            return _storageBoosterCoordinator != null &&
                   _storageBoosterCoordinator.TryApply(_session);
        }

        private void CreateCoordinators()
        {
            _packageDeliveryCoordinator = new(
                _sessionGuard, _gameplayMotionPresenter, _gameplayAudioPresenter, _requiredPackageLifecycleUseCase,
                _requiredPackageGroupView, _foodVisualResolver, _gameplayEvents);
            _waitingRackPlacementCoordinator = new(_sessionGuard, _gameplayMotionPresenter, _waitingRackView);
            _waitingRackAutoFillCoordinator = new(
                _sessionGuard, _requiredPackageLifecycleUseCase, _waitingRackView, _packageDeliveryCoordinator);
            _topTrayMoveCoordinator = new(
                _sessionGuard, _gameplayMotionPresenter, _gameplayAudioPresenter, _boardLayoutView);
            _grillCompletionCoordinator = new(
                _sessionGuard, _gameplayMotionPresenter, _boardLayoutView);
            _comboCoordinator = new(_sessionGuard, _gameplayEvents, _gameplayAudioPresenter);
            _plateBoosterCoordinator = new(_sessionGuard, _waitingRackView);
            _storageBoosterCoordinator = new(
                _sessionGuard,
                _boardLayoutView,
                _packageDeliveryCoordinator,
                _topTrayMoveCoordinator,
                TryResolveWin);
        }

        private void SubscribeCoordinatorEvents()
        {
            _packageDeliveryCoordinator.PackageCompletionStarted += HandlePackageCompletionStarted;
            _packageDeliveryCoordinator.PackageReplaced += HandlePackageReplaced;
            _packageDeliveryCoordinator.PackageDeliveryFailed += HandleGameplayFlowFailed;
            _waitingRackAutoFillCoordinator.AutoFillFinished += HandleAutoFillFinished;
            _waitingRackAutoFillCoordinator.AutoFillFailed += HandleGameplayFlowFailed;
            _grillCompletionCoordinator.GrillCloseFinished += HandleGrillCloseFinished;
        }

        private void UnsubscribeCoordinatorEvents()
        {
            if (_packageDeliveryCoordinator != null)
            {
                _packageDeliveryCoordinator.PackageCompletionStarted -= HandlePackageCompletionStarted;
                _packageDeliveryCoordinator.PackageReplaced -= HandlePackageReplaced;
                _packageDeliveryCoordinator.PackageDeliveryFailed -= HandleGameplayFlowFailed;
            }

            if (_waitingRackAutoFillCoordinator != null)
            {
                _waitingRackAutoFillCoordinator.AutoFillFinished -= HandleAutoFillFinished;
                _waitingRackAutoFillCoordinator.AutoFillFailed -= HandleGameplayFlowFailed;
            }

            if (_grillCompletionCoordinator != null)
            {
                _grillCompletionCoordinator.GrillCloseFinished -= HandleGrillCloseFinished;
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

            if (_gameplayAudioPresenter == null)
            {
                Debug.LogError("GameplayAudioPresenter has not been constructed.");
                return false;
            }

            if (_gameplayWorldClickSfx == null)
            {
                Debug.LogError("GameplayWorldClickSfx has not been constructed.");
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

            _gameplayAudioPresenter.PlayFoodSelected();

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
            _grillCompletionCoordinator.TryCloseCompletedGrill(context.Address.GrillPositionIndex, session);
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
            _grillCompletionCoordinator.TryCloseCompletedGrill(context.Address.GrillPositionIndex, session);
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
                ShowReviveFlow(session);
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

        private void HandlePackageCompletionStarted(GameplaySession session, Vector3 worldPosition)
        {
            if (IsCurrentSession(session))
            {
                _comboCoordinator.RegisterPackageCompleted(session);
                _uiManager.ShowComboFeedback(worldPosition);
            }
        }

        private void HandleAutoFillFinished(GameplaySession session)
        {
            TryResolveWin(session);
        }

        private void HandleGrillCloseFinished(GameplaySession session)
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
                _grillCompletionCoordinator.HasActiveMotion(session) ||
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

            _gameplayWorldClickSfx.StopListening();
            _gameplayEvents.OnLevelEnded(new LevelEndedEvent(session.LevelNumber, true, WinReason));
            _uiManager.ShowWinPopup(OnWinRewardClicked, OnWinRewardClicked);
        }

        private void ShowReviveFlow(GameplaySession session)
        {
            if (!IsCurrentSession(session) || session.State != LevelSessionState.AwaitingRevive)
            {
                return;
            }

            _gameplayWorldClickSfx.StopListening();
            _uiManager.ShowRevivePopup(OnTryAgainClicked, OnHomeClicked);
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

        private bool IsValidComboDuration()
        {
            return _comboDurationSeconds > 0f &&
                   !float.IsNaN(_comboDurationSeconds) &&
                   !float.IsInfinity(_comboDurationSeconds);
        }

        private void OnWinRewardClicked()
        {
            GameplaySession session = _session;

            if (session == null)
            {
                return;
            }

            _navigationActions?.WinRewardClaimed.Invoke(session.LevelNumber);
        }

        private void OnTryAgainClicked()
        {
            if (_session == null)
            {
                return;
            }

            int levelNumber = _session.LevelNumber;
            FinalizeLose();
            _navigationActions?.RetryRequested.Invoke(levelNumber);
        }

        private void OnHomeClicked()
        {
            FinalizeLose();
            _navigationActions?.HomeRequested.Invoke();
        }
    }
}
