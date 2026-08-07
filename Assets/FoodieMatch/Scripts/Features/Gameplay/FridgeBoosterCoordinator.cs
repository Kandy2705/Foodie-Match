using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class FridgeBoosterCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly FridgeBoosterView _view;
        private readonly WaitingRackView _waitingRackView;
        private readonly FoodItemViewPool _foodItemViewPool;
        private readonly RequiredPackageLifecycleUseCase
            _packageLifecycleUseCase;
        private readonly PackageDeliveryCoordinator
            _packageDeliveryCoordinator;
        private readonly FoodVisualResolver
            _foodVisualResolver;

        private GameplaySession _activeSession;
        private FridgeOperationState _operationState;
        private bool _releaseRetryRequested;

        public FridgeBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            FridgeBoosterView view,
            WaitingRackView waitingRackView,
            FoodItemViewPool foodItemViewPool,
            RequiredPackageLifecycleUseCase
                packageLifecycleUseCase,
            PackageDeliveryCoordinator
                packageDeliveryCoordinator,
            FoodVisualResolver foodVisualResolver)
        {
            _sessionGuard = sessionGuard;
            _view = view;
            _waitingRackView = waitingRackView;
            _foodItemViewPool = foodItemViewPool;
            _packageLifecycleUseCase =
                packageLifecycleUseCase;
            _packageDeliveryCoordinator =
                packageDeliveryCoordinator;
            _foodVisualResolver =
                foodVisualResolver;
        }

        public void BeginSession()
        {
            _activeSession = null;
            _operationState = FridgeOperationState.Idle;
            _releaseRetryRequested = false;

            _view.HideImmediately();
        }

        public bool TryApply(GameplaySession session)
        {
            if (_operationState != FridgeOperationState.Idle ||
                !CanContinue(session) ||
                !session.IsInputEnabled)
            {
                return false;
            }

            if (session.WaitingRack.OccupiedCount <= 0)
            {
                Debug.Log(
                    "Fridge booster cannot run because " +
                    "Waiting Rack is empty.");

                return false;
            }

            if (session.FridgeInventory == null)
            {
                if (!session.TryActivateFridgeInventory(out _))
                {
                    Debug.Log(
                        "Fridge inventory could not be created.");

                    return false;
                }
            }

            bool shouldPlayEnter =
                !_view.IsVisible;

            _activeSession = session;
            _operationState = FridgeOperationState.Applying;
            _releaseRetryRequested = false;

            session.DisableInput();

            _ = ApplySafelyAsync(
                session,
                shouldPlayEnter);

            return true;
        }

        public void StartOrRequestRelease(
            GameplaySession session)
        {
            if (!ReferenceEquals(
                    _activeSession,
                    session) ||
                !CanContinue(session) ||
                !session.HasActivatedFridgeBooster ||
                session.FridgeInventory == null ||
                session.FridgeInventory.IsEmpty)
            {
                return;
            }

            if (_operationState != FridgeOperationState.Idle)
            {
                _releaseRetryRequested = true;
                return;
            }

            _operationState = FridgeOperationState.Releasing;

            _ = ReleaseSafelyAsync(session);
        }

        public void EndSession()
        {
            GameplaySession session =
                _activeSession;

            _activeSession = null;
            _operationState = FridgeOperationState.Idle;
            _releaseRetryRequested = false;

            _view.CancelAnimations();
            _view.HideImmediately();

            session?.ClearFridgeInventory();
        }

        public bool IsBusy(GameplaySession session)
        {
            return ReferenceEquals(_activeSession, session) &&
                   _operationState != FridgeOperationState.Idle;
        }

        private async Task ApplySafelyAsync(
            GameplaySession session,
            bool shouldPlayEnter)
        {
            bool succeeded = false;

            try
            {
                succeeded = await ApplyAsync(session, shouldPlayEnter);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (ReferenceEquals(_activeSession, session))
                {
                    _operationState = FridgeOperationState.Idle;

                    bool shouldRetry =
                        _releaseRetryRequested &&
                        succeeded &&
                        CanContinue(session) &&
                        session.FridgeInventory != null &&
                        !session.FridgeInventory.IsEmpty;

                    _releaseRetryRequested = false;

                    if (shouldRetry)
                    {
                        StartOrRequestRelease(session);
                    }
                }
            }
        }

        private async Task<bool> ApplyAsync(
            GameplaySession session,
            bool shouldPlayEnter)
        {
            if (shouldPlayEnter)
            {
                await _view.PlayEnterAndOpenAsync();

                if (!CanContinue(session))
                {
                    return false;
                }
            }
            else
            {
                if (!_view.IsVisible)
                {
                    Debug.LogWarning(
                        "Fridge became hidden before scoop started.");

                    return false;
                }
            }

            List<Task<bool>> pendingFoodEntries =
                new();

            Vector3 spoonRestPosition =
                _waitingRackView.GetFoodAnchorWorldPositionAt(0);

            await _view.PlaySpoonAppearAsync(
                spoonRestPosition);

            if (!CanContinue(session))
            {
                return false;
            }

            for (int slotIndex = 0;
                 slotIndex < session.WaitingRack.Capacity;
                 slotIndex++)
            {
                if (!CanContinue(session))
                {
                    return false;
                }

                int foodTokenId =
                    session.WaitingRack
                        .GetFoodTokenIdAt(slotIndex);

                if (foodTokenId <= 0)
                {
                    continue;
                }

                bool scoopStarted =
                    await TryStartScoopSlotAsync(
                        session,
                        slotIndex,
                        foodTokenId,
                        pendingFoodEntries);

                if (!scoopStarted)
                {
                    Debug.LogError(
                        $"Fridge stopped at Waiting Rack " +
                        $"slot {slotIndex}.");

                    break;
                }
            }

            Task spoonReturnTask =
                _view.PlaySpoonReturnAsync(
                    spoonRestPosition);

            bool[] enterResults =
                await Task.WhenAll(
                    pendingFoodEntries);

            await spoonReturnTask;

            for (int i = 0;
                 i < enterResults.Length;
                 i++)
            {
                if (!enterResults[i])
                {
                    return false;
                }
            }

            if (!CanContinue(session))
            {
                return false;
            }

            UpdateFridgeVisualState(session);

            session.StartPlaying();

            Debug.Log(
                $"Fridge scoop completed. Stored food: " +
                $"{session.FridgeInventory.Count}");

            if (!CanContinue(session) ||
                session.FridgeInventory.IsEmpty)
            {
                return true;
            }

            _operationState = FridgeOperationState.Releasing;
            return await ReleaseAvailableMatchesCoreAsync(session);
        }

        private async Task ReleaseSafelyAsync(
            GameplaySession session)
        {
            bool succeeded = false;

            try
            {
                succeeded = await ReleaseAvailableMatchesCoreAsync(session);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (ReferenceEquals(
                        _activeSession,
                        session))
                {
                    _operationState = FridgeOperationState.Idle;

                    bool shouldRetry =
                        _releaseRetryRequested &&
                        succeeded &&
                        CanContinue(session) &&
                        session.FridgeInventory != null &&
                        !session.FridgeInventory.IsEmpty;

                    _releaseRetryRequested = false;

                    if (shouldRetry)
                    {
                        StartOrRequestRelease(session);
                    }
                }
            }
        }

        private async Task<bool>
            ReleaseAvailableMatchesCoreAsync(
                GameplaySession session)
        {
            do
            {
                _releaseRetryRequested = false;

                while (CanContinue(session) &&
                       session.FridgeInventory != null &&
                       !session.FridgeInventory.IsEmpty)
                {
                    if (!_packageLifecycleUseCase
                        .TryFindFridgeMatch(
                            session.FridgeInventory,
                            session.RequiredPackages,
                            out FridgeTransfer transfer))
                    {
                        break;
                    }

                    bool releaseSucceeded =
                        await TryReleaseOneFoodAsync(
                            session,
                            transfer);

                    if (!releaseSucceeded)
                    {
                        await UpdateFridgeVisualStateAsync(session);
                        return false;
                    }
                }
            }
            while (_releaseRetryRequested &&
                   CanContinue(session));

            await UpdateFridgeVisualStateAsync(session);
            return true;
        }

        private async Task<bool>
            TryReleaseOneFoodAsync(
                GameplaySession session,
                FridgeTransfer transfer)
        {
            Sprite foodSprite = _foodVisualResolver.ResolveIcon(transfer.FoodTokenId);

            FoodItemView foodItemView = _foodItemViewPool.Get(
                null,
                _view.GetFridgeEntryWorldPosition(),
                Quaternion.identity);
            foodItemView.Setup(transfer.FoodTokenId, foodSprite);
            foodItemView.SetInteractable(false);

            if (!_packageDeliveryCoordinator
                .TryCreateFridgeFlight(
                    transfer,
                    foodItemView,
                    session,
                    out _))
            {
                ReleaseFood(foodItemView);

                Debug.LogError(
                    "Fridge package flight could not " +
                    "be prepared.");

                return false;
            }

            _view.SetFullState();

            Vector3 targetScale =
                await _view.PlayReleasePopAsync(
                    foodItemView);

            if (!CanContinue(session))
            {
                ReleaseFood(foodItemView);
                return false;
            }

            if (!_packageDeliveryCoordinator
                .TryCreateFridgeFlight(
                    transfer,
                    foodItemView,
                    session,
                    out PackageFlight flight))
            {
                ReleaseFood(foodItemView);
                return false;
            }

            if (!_packageLifecycleUseCase
                .TryMoveFoodFromFridge(
                    transfer,
                    session.FridgeInventory,
                    session.RequiredPackages))
            {
                ReleaseFood(foodItemView);

                if (session.FridgeInventory != null &&
                    !session.FridgeInventory.IsEmpty)
                {
                    _view.SetFullState();
                }

                Debug.LogError(
                    "Fridge food could not be moved " +
                    "to its required package.");

                return false;
            }

            _packageDeliveryCoordinator
                .IncreaseServedFoodCount(session);

            Task fridgePulseTask =
                _view.PlayFridgeBumpAsync();

            Task growTask =
                _view.PlayReleaseGrowAsync(
                    foodItemView,
                    targetScale);

            Task<bool> deliveryTask =
                _packageDeliveryCoordinator
                    .DeliverBatchAsync(
                        new[] { flight },
                        session);

            await Task.WhenAll(
                fridgePulseTask,
                growTask,
                deliveryTask);

            bool delivered =
                await deliveryTask;

            if (!delivered)
            {
                Debug.LogError(
                    "Fridge food delivery failed.");

                return false;
            }

            Debug.Log(
                $"Fridge released token " +
                $"{transfer.FoodTokenId} to package " +
                $"{transfer.PackageIndex}.");

            return true;
        }

        private async Task<bool> TryStartScoopSlotAsync(
            GameplaySession session,
            int slotIndex,
            int expectedFoodTokenId,
            List<Task<bool>> pendingFoodEntries)
        {
            if (!_waitingRackView.TryGetFoodAt(
                    slotIndex,
                    out FoodItemView foodItemView) ||
                foodItemView == null)
            {
                Debug.LogError(
                    $"Fridge could not find food visual " +
                    $"at slot {slotIndex}.");

                return false;
            }

            if (foodItemView.FoodTokenId !=
                expectedFoodTokenId)
            {
                Debug.LogError(
                    $"Fridge token mismatch at slot " +
                    $"{slotIndex}. Model: " +
                    $"{expectedFoodTokenId}, View: " +
                    $"{foodItemView.FoodTokenId}.");

                return false;
            }

            Vector3 waitingRackWorldPosition =
                _waitingRackView.GetFoodAnchorWorldPositionAt(
                    slotIndex);

            bool modelRemoved = false;
            bool viewRemoved = false;
            bool foodEntryStarted = false;

            try
            {
                if (!session.WaitingRack
                    .TryRemoveFoodAt(
                        slotIndex,
                        out int removedTokenId) ||
                    removedTokenId !=
                        expectedFoodTokenId)
                {
                    return false;
                }

                modelRemoved = true;

                FoodItemView removedView =
                    _waitingRackView.RemoveFoodAt(
                        slotIndex);

                if (removedView == null ||
                    removedView != foodItemView)
                {
                    if (removedView != null)
                    {
                        _waitingRackView.RestoreFoodAt(
                            slotIndex,
                            removedView);
                    }

                    RestoreModelFood(
                        session,
                        slotIndex,
                        expectedFoodTokenId);

                    return false;
                }

                viewRemoved = true;
                foodItemView.SetInteractable(false);

                Vector3 originalScale =
                    foodItemView.transform.localScale;

                await _view.PlaySpoonMoveToFoodAsync(
                    waitingRackWorldPosition);

                await _view.PlaySpoonLowerAsync(
                    waitingRackWorldPosition);

                if (!CanContinue(session))
                {
                    foodItemView.transform.localScale =
                        originalScale;

                    RollbackScoop(
                        session,
                        slotIndex,
                        expectedFoodTokenId,
                        foodItemView,
                        modelRemoved,
                        viewRemoved);

                    return false;
                }

                Task<bool> enterTask =
                    CompleteFoodEnterAsync(
                        session,
                        slotIndex,
                        expectedFoodTokenId,
                        foodItemView,
                        originalScale);

                pendingFoodEntries.Add(enterTask);
                foodEntryStarted = true;

                await _view.PlaySpoonLiftAsync(
                    waitingRackWorldPosition);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (!foodEntryStarted)
                {
                    RollbackScoop(
                        session,
                        slotIndex,
                        expectedFoodTokenId,
                        foodItemView,
                        modelRemoved,
                        viewRemoved);
                }

                return false;
            }
        }

        private async Task<bool> CompleteFoodEnterAsync(
            GameplaySession session,
            int slotIndex,
            int foodTokenId,
            FoodItemView foodItemView,
            Vector3 originalScale)
        {
            try
            {
                await _view.PlayFoodFlightAsync(
                    foodItemView);

                if (!CanContinue(session))
                {
                    if (foodItemView != null)
                    {
                        foodItemView.transform.localScale =
                            originalScale;
                    }

                    RollbackScoop(
                        session,
                        slotIndex,
                        foodTokenId,
                        foodItemView,
                        modelWasRemoved: true,
                        viewWasRemoved: true);

                    return false;
                }

                session.FridgeInventory.Store(
                    foodTokenId);

                _view.SetFullState();

                if (foodItemView != null)
                {
                    _foodItemViewPool.Release(foodItemView);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (foodItemView != null)
                {
                    foodItemView.transform.localScale =
                        originalScale;
                }

                RollbackScoop(
                    session,
                    slotIndex,
                    foodTokenId,
                    foodItemView,
                    modelWasRemoved: true,
                    viewWasRemoved: true);

                return false;
            }
        }

        private void RollbackScoop(
            GameplaySession session,
            int slotIndex,
            int foodTokenId,
            FoodItemView foodItemView,
            bool modelWasRemoved,
            bool viewWasRemoved)
        {
            if (modelWasRemoved)
            {
                RestoreModelFood(
                    session,
                    slotIndex,
                    foodTokenId);
            }

            if (viewWasRemoved &&
                foodItemView != null)
            {
                if (!_waitingRackView.RestoreFoodAt(
                        slotIndex,
                        foodItemView))
                {
                    Debug.LogError(
                        $"Fridge failed to restore food " +
                        $"visual at slot {slotIndex}.");
                }
            }
        }

        private static void RestoreModelFood(
            GameplaySession session,
            int slotIndex,
            int foodTokenId)
        {
            if (!session.WaitingRack
                .TryRestoreFoodAt(
                    slotIndex,
                    foodTokenId))
            {
                Debug.LogError(
                    $"Fridge failed to restore model food " +
                    $"at slot {slotIndex}.");
            }
        }

        private void UpdateFridgeVisualState(
            GameplaySession session)
        {
            if (session.FridgeInventory != null &&
                !session.FridgeInventory.IsEmpty)
            {
                _view.SetFullState();
            }
            else
            {
                _view.SetClosedState();
            }
        }

        private async Task UpdateFridgeVisualStateAsync(
            GameplaySession session)
        {
            if (session.FridgeInventory != null &&
                !session.FridgeInventory.IsEmpty)
            {
                _view.SetFullState();
                return;
            }

            await _view.PlayDisappearAsync();
        }

        private void ReleaseFood(FoodItemView foodItemView)
        {
            if (foodItemView != null)
            {
                _foodItemViewPool.Release(foodItemView);
            }
        }

        private bool CanContinue(
            GameplaySession session)
        {
            return _sessionGuard.IsCurrentSession(
                       session.SessionId) &&
                   session.CanContinueGameplay;
        }

        private enum FridgeOperationState
        {
            Idle,
            Applying,
            Releasing
        }
    }
}
