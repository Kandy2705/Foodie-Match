using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Events;
using FoodieMatch.Core.Application.GameState;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
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

        private readonly GameplaySessionGuard _sessionGuard =
            new GameplaySessionGuard();

        private UIManager _uiManager;
        private GameplayEvents _gameplayEvents;
        private BoardLayoutView _boardLayoutView;
        private RequiredPackageGroupView _requiredPackageGroupView;
        private WaitingRackView _waitingRackView;
        private GameplayMotionPresenter _gameplayMotionPresenter;
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
        private LevelProgressModel _levelProgress;
        private PackageMotionState[] _packageMotionStates;
        private int _displayedServedCount;
        private int _currentLevelNumber;
        private LevelSessionState _levelSessionState;
        private bool _isInputEnabled;

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

            _levelProgress = new LevelProgressModel(
                _board.RemainingFoodCount);
            _displayedServedCount = 0;
            CreatePackageMotionStates();

            _sessionGuard.BeginSession();
            _gameplayMotionPresenter.CancelAllMotions();

            _boardLayoutView.Setup(_board);
            _waitingRackView.Clear();
            RefreshRequiredPackageViews();
            _levelSessionState = LevelSessionState.Playing;
            _isInputEnabled = true;

            Debug.Log($"Start Level {levelNumber}");

            _gameplayEvents.OnLevelStarted(
                new LevelStartedEvent(levelNumber));
            _gameplayEvents.OnLevelProgressChanged(
                new LevelProgressChangedEvent(
                    _levelProgress.ServedCount,
                    _levelProgress.TotalCount));
        }

        private void ResolveWin()
        {
            if (!HasDependencies() ||
                _levelSessionState != LevelSessionState.Playing)
            {
                return;
            }

            _levelSessionState = LevelSessionState.Won;
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

        public void ClearLevel()
        {
            _sessionGuard.EndSession();
            _gameplayMotionPresenter?.CancelAllMotions();

            _levelProgress = null;
            _packageMotionStates = null;
            _displayedServedCount = 0;
            _levelSessionState = LevelSessionState.None;
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

            if (_gameplayMotionPresenter == null)
            {
                Debug.LogError(
                    "GameplayMotionPresenter has not been constructed.");
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
            _sessionGuard.EndSession();
            _gameplayMotionPresenter?.CancelAllMotions();

            if (_boardLayoutView != null)
            {
                _boardLayoutView.FoodSelected -= HandleFoodSelected;
            }
        }

        private void HandleFoodSelected(FoodSelectionContext context)
        {
            _ = ProcessFoodSelectionSafelyAsync(context);
        }

        private async Task ProcessFoodSelectionSafelyAsync(
            FoodSelectionContext context)
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

        private async Task ProcessFoodSelectionAsync(
            FoodSelectionContext context)
        {
            if (_levelSessionState != LevelSessionState.Playing ||
                !_isInputEnabled ||
                context.FoodItemView == null ||
                _requiredPackages == null ||
                _waitingRack == null)
            {
                return;
            }

            int sessionId = _sessionGuard.CurrentSessionId;
            SelectFoodResult result = _selectFoodUseCase.Execute(
                context.Address,
                _board,
                _requiredPackages,
                _waitingRack);

            if (!result.IsPlaced)
            {
                return;
            }

            if (result.Type ==
                SelectFoodResultType.PlacedInRequiredPackage)
            {
                await ProcessRequiredPackageSelectionAsync(
                    context,
                    result,
                    sessionId);

                return;
            }

            await ProcessWaitingRackSelectionAsync(
                context,
                result,
                sessionId);
        }

        private async Task ProcessRequiredPackageSelectionAsync(
            FoodSelectionContext context,
            SelectFoodResult result,
            int sessionId)
        {
            FoodItemView foodItemView = context.FoodItemView;
            _boardLayoutView.ReleaseFoodItem(foodItemView);
            RegisterFoodInModelProgress();
            MoveTopTrayToGrill(
                context.Address.GrillPositionIndex);

            if (!TryCreatePackageFlight(
                    foodItemView,
                    result.TargetIndex,
                    out PackageFlight flight))
            {
                Debug.LogError(
                    "Required package flight could not be created.");

                foodItemView.Clear();

                if (_requiredPackages != null &&
                    result.TargetIndex >= 0 &&
                    result.TargetIndex < _requiredPackages.Length)
                {
                    RefreshRequiredPackageViewAt(result.TargetIndex);
                }

                ShowFoodInProgress();
                _isInputEnabled = false;
                return;
            }

            PackageMotionState motionState =
                _packageMotionStates[flight.PackageIndex];

            if (!motionState.TryRegisterIncomingFlight(
                    flight.ExpectedPackage))
            {
                Debug.LogError(
                    $"Required package {flight.PackageIndex} " +
                    "could not register an incoming flight.");

                ReconcileFailedPackageFlight(flight);
                ShowFoodInProgress();
                _isInputEnabled = false;
                return;
            }

            MotionResult motionResult;

            try
            {
                motionResult = await _gameplayMotionPresenter
                    .MoveFoodToRequiredPackageAsync(
                        flight.FoodItemView,
                        flight.PackageIndex,
                        flight.RequiredAmount,
                        flight.FilledSlotIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }
            finally
            {
                if (!motionState.TryCompleteIncomingFlight(
                        flight.ExpectedPackage))
                {
                    Debug.LogError(
                        $"Required package {flight.PackageIndex} " +
                        "could not complete an incoming flight.");
                }
            }

            if (!CanContinueGameplay(sessionId) ||
                !IsExpectedPackage(flight))
            {
                return;
            }

            if (motionResult == MotionResult.Cancelled)
            {
                return;
            }

            if (motionResult == MotionResult.Failed)
            {
                Debug.LogError(
                    $"Food flight to required package " +
                    $"{flight.PackageIndex} failed.");

                ReconcileFailedPackageFlight(flight);
            }

            ShowFoodInProgress();
            TryStartPackageCompletion(
                flight.PackageIndex,
                flight.ExpectedPackage,
                sessionId);
            TryResolveWin(sessionId);
        }

        private async Task ProcessWaitingRackSelectionAsync(
            FoodSelectionContext context,
            SelectFoodResult result,
            int sessionId)
        {
            FoodItemView foodItemView = context.FoodItemView;
            _boardLayoutView.ReleaseFoodItem(foodItemView);

            Task<MotionResult> motionTask =
                MoveFoodToWaitingRackSafelyAsync(
                    foodItemView,
                    result.TargetIndex);

            MoveTopTrayToGrill(
                context.Address.GrillPositionIndex);

            bool causedWaitingRackFull = _waitingRack.IsFull;

            if (causedWaitingRackFull)
            {
                EnterAwaitingRevive();
            }

            MotionResult motionResult = await motionTask;

            if (!_sessionGuard.IsCurrentSession(sessionId))
            {
                return;
            }

            if (motionResult == MotionResult.Failed)
            {
                Debug.LogError(
                    $"Food flight to waiting rack slot " +
                    $"{result.TargetIndex} failed.");

                if (!ReconcileWaitingRackPlacement(
                        result.TargetIndex,
                        foodItemView))
                {
                    _isInputEnabled = false;
                    return;
                }
            }

            if (motionResult == MotionResult.Cancelled)
            {
                return;
            }

            if (causedWaitingRackFull &&
                _levelSessionState == LevelSessionState.AwaitingRevive)
            {
                ShowLosePopup();
            }
        }

        private async Task<MotionResult> MoveFoodToWaitingRackSafelyAsync(
            FoodItemView foodItemView,
            int rackSlotIndex)
        {
            try
            {
                return await _gameplayMotionPresenter
                    .MoveFoodToWaitingRackAsync(
                        foodItemView,
                        rackSlotIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return MotionResult.Failed;
            }
        }

        private bool ReconcileWaitingRackPlacement(
            int rackSlotIndex,
            FoodItemView foodItemView)
        {
            if (_waitingRackView.CompleteFoodPlacementAt(
                    rackSlotIndex,
                    foodItemView))
            {
                return true;
            }

            return _waitingRackView.SetFoodAt(
                rackSlotIndex,
                foodItemView);
        }

        private bool TryCreatePackageFlight(
            FoodItemView foodItemView,
            int packageIndex,
            out PackageFlight flight)
        {
            flight = default;

            if (foodItemView == null ||
                _requiredPackages == null ||
                _packageMotionStates == null ||
                packageIndex < 0 ||
                packageIndex >= _requiredPackages.Length ||
                packageIndex >= _packageMotionStates.Length)
            {
                return false;
            }

            RequiredPackageModel requiredPackage =
                _requiredPackages[packageIndex];

            if (requiredPackage == null ||
                requiredPackage.FilledAmount <= 0)
            {
                return false;
            }

            flight = new PackageFlight(
                foodItemView,
                requiredPackage,
                packageIndex,
                requiredPackage.RequiredAmount,
                requiredPackage.FilledAmount - 1);

            return true;
        }

        private void ReconcileFailedPackageFlight(
            PackageFlight flight)
        {
            flight.FoodItemView.Clear();
            RefreshRequiredPackageViewAt(flight.PackageIndex);
        }

        private void RegisterFoodInModelProgress()
        {
            if (_levelProgress == null ||
                !_levelProgress.TryServeFood())
            {
                Debug.LogError("Level progress could not serve food.");
            }
        }

        private void ShowFoodInProgress()
        {
            if (_levelProgress == null ||
                _displayedServedCount >= _levelProgress.ServedCount)
            {
                return;
            }

            _displayedServedCount++;
            _gameplayEvents.OnLevelProgressChanged(
                new LevelProgressChangedEvent(
                    _displayedServedCount,
                    _levelProgress.TotalCount));
        }

        private void TryStartPackageCompletion(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            int sessionId)
        {
            if (!CanContinueGameplay(sessionId) ||
                !TryGetPackageMotionState(
                    packageIndex,
                    expectedPackage,
                    out PackageMotionState motionState) ||
                !expectedPackage.IsComplete ||
                motionState.IncomingFlightCount != 0 ||
                motionState.IsCompleteMotionRunning)
            {
                return;
            }

            motionState.IsCompleteMotionRunning = true;
            _ = CompletePackageSafelyAsync(
                packageIndex,
                expectedPackage,
                sessionId);
        }

        private async Task CompletePackageSafelyAsync(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            int sessionId)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await _gameplayMotionPresenter
                    .PlayRequiredPackageCompleteAsync(packageIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }

            if (!CanContinueGameplay(sessionId) ||
                !TryGetPackageMotionState(
                    packageIndex,
                    expectedPackage,
                    out PackageMotionState motionState))
            {
                return;
            }

            if (motionResult == MotionResult.Cancelled)
            {
                motionState.IsCompleteMotionRunning = false;
                return;
            }

            if (motionResult == MotionResult.Failed)
            {
                Debug.LogError(
                    $"Required package {packageIndex} complete " +
                    "feedback failed.");
            }

            if (!_requiredPackageLifecycleUseCase
                    .TryReplaceCompletedPackage(
                        packageIndex,
                        _board,
                        _waitingRack,
                        _requiredPackages,
                        _requiredPackageGenerationSettings,
                        out RequiredPackageModel newPackage))
            {
                motionState.IsCompleteMotionRunning = false;
                Debug.LogError(
                    $"Required package {packageIndex} could not be replaced.");

                return;
            }

            motionState.Reset(newPackage);
            RefreshRequiredPackageViewAt(packageIndex);
            TryResolveWin(sessionId);
        }

        private bool TryGetPackageMotionState(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            out PackageMotionState motionState)
        {
            motionState = null;

            if (_requiredPackages == null ||
                _packageMotionStates == null ||
                packageIndex < 0 ||
                packageIndex >= _requiredPackages.Length ||
                packageIndex >= _packageMotionStates.Length ||
                _requiredPackages[packageIndex] != expectedPackage)
            {
                return false;
            }

            motionState = _packageMotionStates[packageIndex];
            return motionState != null &&
                   motionState.Package == expectedPackage;
        }

        private bool IsExpectedPackage(PackageFlight flight)
        {
            return TryGetPackageMotionState(
                flight.PackageIndex,
                flight.ExpectedPackage,
                out _);
        }

        private bool CanContinueGameplay(int sessionId)
        {
            return _sessionGuard.IsCurrentSession(sessionId) &&
                   _levelSessionState == LevelSessionState.Playing;
        }

        private void TryResolveWin(int sessionId)
        {
            if (!CanContinueGameplay(sessionId) ||
                _levelProgress == null ||
                !_levelProgress.IsComplete ||
                _displayedServedCount < _levelProgress.ServedCount ||
                HasActivePackageMotion())
            {
                return;
            }

            ResolveWin();
        }

        private bool HasActivePackageMotion()
        {
            if (_packageMotionStates == null)
            {
                return false;
            }

            for (int i = 0; i < _packageMotionStates.Length; i++)
            {
                PackageMotionState motionState =
                    _packageMotionStates[i];

                if (motionState != null &&
                    (motionState.IncomingFlightCount > 0 ||
                     motionState.IsCompleteMotionRunning ||
                     motionState.Package != null &&
                     motionState.Package.IsComplete))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreatePackageMotionStates()
        {
            _packageMotionStates = new PackageMotionState[
                _requiredPackages.Length];

            for (int i = 0; i < _requiredPackages.Length; i++)
            {
                _packageMotionStates[i] = new PackageMotionState(
                    _requiredPackages[i]);
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

        private void EnterAwaitingRevive()
        {
            if (_levelSessionState != LevelSessionState.Playing)
            {
                return;
            }

            _levelSessionState = LevelSessionState.AwaitingRevive;
            _isInputEnabled = false;
        }

        private void ShowLosePopup()
        {
            if (_levelSessionState != LevelSessionState.AwaitingRevive)
            {
                return;
            }

            _uiManager.ShowLosePopup(
                OnTryAgainClicked,
                OnHomeClicked);
        }

        private void FinalizeLose()
        {
            if (_levelSessionState != LevelSessionState.AwaitingRevive)
            {
                return;
            }

            _levelSessionState = LevelSessionState.Lost;

            _gameplayEvents.OnLevelEnded(
                new LevelEndedEvent(
                    _currentLevelNumber,
                    false,
                    LoseReason));
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

        private void OnTryAgainClicked()
        {
            FinalizeLose();

            _uiManager.HideAllPopups();
            StartLevel(_currentLevelNumber, _homeRequested);
        }

        private void OnHomeClicked()
        {
            FinalizeLose();

            _uiManager.HideAllPopups();
            ClearLevel();

            _homeRequested?.Invoke();
        }

        private readonly struct PackageFlight
        {
            public PackageFlight(
                FoodItemView foodItemView,
                RequiredPackageModel expectedPackage,
                int packageIndex,
                int requiredAmount,
                int filledSlotIndex)
            {
                FoodItemView = foodItemView;
                ExpectedPackage = expectedPackage;
                PackageIndex = packageIndex;
                RequiredAmount = requiredAmount;
                FilledSlotIndex = filledSlotIndex;
            }

            public FoodItemView FoodItemView { get; }
            public RequiredPackageModel ExpectedPackage { get; }
            public int PackageIndex { get; }
            public int RequiredAmount { get; }
            public int FilledSlotIndex { get; }
        }

        private sealed class PackageMotionState
        {
            public PackageMotionState(RequiredPackageModel package)
            {
                Package = package;
            }

            public RequiredPackageModel Package { get; private set; }
            public int IncomingFlightCount { get; private set; }
            public bool IsCompleteMotionRunning { get; set; }

            public bool TryRegisterIncomingFlight(
                RequiredPackageModel expectedPackage)
            {
                if (Package != expectedPackage ||
                    IsCompleteMotionRunning)
                {
                    return false;
                }

                IncomingFlightCount++;
                return true;
            }

            public bool TryCompleteIncomingFlight(
                RequiredPackageModel expectedPackage)
            {
                if (Package != expectedPackage ||
                    IncomingFlightCount <= 0)
                {
                    return false;
                }

                IncomingFlightCount--;
                return true;
            }

            public void Reset(RequiredPackageModel package)
            {
                Package = package;
                IncomingFlightCount = 0;
                IsCompleteMotionRunning = false;
            }
        }
    }
}
