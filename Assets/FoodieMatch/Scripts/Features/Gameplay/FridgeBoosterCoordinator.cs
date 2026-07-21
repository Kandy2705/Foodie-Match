using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Features.Board;
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
        private readonly BoardLayoutView _boardLayoutView;
        private readonly RequiredPackageLifecycleUseCase
            _packageLifecycleUseCase;
        private readonly PackageDeliveryCoordinator
            _packageDeliveryCoordinator;
        private readonly FoodVisualResolver
            _foodVisualResolver;

        private GameplaySession _activeSession;

        private bool _isApplying;
        private bool _isReleasing;
        private bool _releaseRetryRequested;
        private bool _hasFailed;

        public FridgeBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            FridgeBoosterView view,
            WaitingRackView waitingRackView,
            BoardLayoutView boardLayoutView,
            RequiredPackageLifecycleUseCase
                packageLifecycleUseCase,
            PackageDeliveryCoordinator
                packageDeliveryCoordinator,
            FoodVisualResolver foodVisualResolver)
        {
            _sessionGuard = sessionGuard;
            _view = view;
            _waitingRackView = waitingRackView;
            _boardLayoutView = boardLayoutView;
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
            _isApplying = false;
            _isReleasing = false;
            _releaseRetryRequested = false;
            _hasFailed = false;

            _view?.HideImmediately();
        }

        public bool TryApply(GameplaySession session)
        {
            if (_isApplying ||
                _isReleasing ||
                _view == null ||
                _waitingRackView == null ||
                _boardLayoutView == null ||
                _packageLifecycleUseCase == null ||
                _packageDeliveryCoordinator == null ||
                _foodVisualResolver == null ||
                !CanContinue(session) ||
                !session.IsInputEnabled)
            {
                return false;
            }

            if (session.WaitingRack == null ||
                session.WaitingRack.OccupiedCount <= 0)
            {
                Debug.Log(
                    "Fridge booster cannot run because " +
                    "Waiting Rack is empty.");

                return false;
            }

            if (session.HasActivatedFridgeBooster)
            {
                Debug.Log(
                    "Fridge booster was already activated.");

                return false;
            }

            if (!session.TryActivateFridgeInventory(
                    out _))
            {
                Debug.Log(
                    "Fridge inventory could not be activated.");

                return false;
            }

            _activeSession = session;
            _isApplying = true;
            _hasFailed = false;

            session.DisableInput();

            _ = ApplySafelyAsync(session);
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

            if (_isApplying || _isReleasing)
            {
                _releaseRetryRequested = true;
                return;
            }

            _isReleasing = true;
            session.DisableInput();

            _ = ReleaseSafelyAsync(session);
        }

        public void EndSession()
        {
            GameplaySession session =
                _activeSession;

            _activeSession = null;
            _isApplying = false;
            _isReleasing = false;
            _releaseRetryRequested = false;
            _hasFailed = false;

            _view?.CancelAnimations();
            _view?.HideImmediately();

            session?.ClearFridgeInventory();
        }

        private async Task ApplySafelyAsync(
            GameplaySession session)
        {
            try
            {
                await ApplyAsync(session);
            }
            catch (Exception exception)
            {
                _hasFailed = true;
                Debug.LogException(exception);
            }
            finally
            {
                if (ReferenceEquals(
                        _activeSession,
                        session))
                {
                    _isApplying = false;

                    if (!_isReleasing &&
                        !_hasFailed &&
                        CanContinue(session))
                    {
                        session.StartPlaying();
                    }
                }
            }
        }

        private async Task ApplyAsync(
            GameplaySession session)
        {
            await _view.PlayEnterAndOpenAsync();

            if (!CanContinue(session))
            {
                return;
            }

            for (int slotIndex =
                     session.WaitingRack.Capacity - 1;
                 slotIndex >= 0;
                 slotIndex--)
            {
                if (!CanContinue(session))
                {
                    return;
                }

                int foodTokenId =
                    session.WaitingRack
                        .GetFoodTokenIdAt(slotIndex);

                if (foodTokenId <= 0)
                {
                    continue;
                }

                bool scoopSucceeded =
                    await TryScoopSlotAsync(
                        session,
                        slotIndex,
                        foodTokenId);

                if (!scoopSucceeded)
                {
                    Debug.LogError(
                        $"Fridge stopped at Waiting Rack " +
                        $"slot {slotIndex}.");

                    break;
                }
            }

            if (!CanContinue(session))
            {
                return;
            }

            UpdateFridgeVisualState(session);

            await _view.PlaySpoonExitLeftAsync();

            Debug.Log(
                $"Fridge scoop completed. Stored food: " +
                $"{session.FridgeInventory?.Count ?? 0}");

            if (!CanContinue(session) ||
                session.FridgeInventory == null ||
                session.FridgeInventory.IsEmpty)
            {
                return;
            }

            _isReleasing = true;

            try
            {
                await ReleaseAvailableMatchesCoreAsync(
                    session);
            }
            finally
            {
                _isReleasing = false;
            }
        }

        private async Task ReleaseSafelyAsync(
            GameplaySession session)
        {
            try
            {
                await ReleaseAvailableMatchesCoreAsync(
                    session);
            }
            catch (Exception exception)
            {
                _hasFailed = true;
                Debug.LogException(exception);
            }
            finally
            {
                if (ReferenceEquals(
                        _activeSession,
                        session))
                {
                    _isReleasing = false;

                    bool shouldRetry =
                        _releaseRetryRequested &&
                        !_hasFailed &&
                        CanContinue(session) &&
                        session.FridgeInventory != null &&
                        !session.FridgeInventory.IsEmpty;

                    _releaseRetryRequested = false;

                    if (shouldRetry)
                    {
                        StartOrRequestRelease(session);
                    }
                    else if (!_hasFailed &&
                             !_isApplying &&
                             CanContinue(session))
                    {
                        session.StartPlaying();
                    }
                }
            }
        }

        private async Task
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
                        break;
                    }
                }
            }
            while (_releaseRetryRequested &&
                   !_hasFailed &&
                   CanContinue(session));

            UpdateFridgeVisualState(session);
        }

        private async Task<bool>
            TryReleaseOneFoodAsync(
                GameplaySession session,
                FridgeTransfer transfer)
        {
            Sprite foodSprite =
                _foodVisualResolver.ResolveIcon(
                    transfer.FoodTokenId);

            if (foodSprite == null)
            {
                Debug.LogError(
                    $"Fridge sprite could not be resolved " +
                    $"for token {transfer.FoodTokenId}.");

                return false;
            }

            FoodItemView foodItemView =
                _boardLayoutView
                    .CreateTransientFoodItemView(
                        foodSprite,
                        _view.GetFridgeEntryWorldPosition(),
                        transfer.FoodTokenId);

            if (foodItemView == null)
            {
                Debug.LogError(
                    "Fridge transient food view " +
                    "could not be created.");

                return false;
            }

            if (!_packageDeliveryCoordinator
                .TryCreateFridgeFlight(
                    transfer,
                    foodItemView,
                    session,
                    out _))
            {
                DestroyTransientFood(foodItemView);

                Debug.LogError(
                    "Fridge package flight could not " +
                    "be prepared.");

                return false;
            }

            await _view.PlayReleasePopAsync(
                foodItemView);

            if (!CanContinue(session))
            {
                DestroyTransientFood(foodItemView);
                return false;
            }

            if (!_packageDeliveryCoordinator
                .TryCreateFridgeFlight(
                    transfer,
                    foodItemView,
                    session,
                    out PackageFlight flight))
            {
                DestroyTransientFood(foodItemView);
                return false;
            }

            if (!_packageLifecycleUseCase
                .TryMoveFoodFromFridge(
                    transfer,
                    session.FridgeInventory,
                    session.RequiredPackages))
            {
                DestroyTransientFood(foodItemView);
                UpdateFridgeVisualState(session);

                Debug.LogError(
                    "Fridge food could not be moved " +
                    "to its required package.");

                return false;
            }

            _packageDeliveryCoordinator
                .IncreaseServedFoodCount(session);

            UpdateFridgeVisualState(session);

            bool delivered =
                await _packageDeliveryCoordinator
                    .DeliverBatchAsync(
                        new[] { flight },
                        session);

            if (!delivered)
            {
                _hasFailed = true;
                DestroyTransientFood(foodItemView);

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

        private async Task<bool> TryScoopSlotAsync(
            GameplaySession session,
            int slotIndex,
            int expectedFoodTokenId)
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
                foodItemView.transform.position;

            bool modelRemoved = false;
            bool viewRemoved = false;

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

                await _view.PlayScoopFoodAsync(
                    foodItemView,
                    waitingRackWorldPosition);

                if (!CanContinue(session))
                {
                    RollbackScoop(
                        session,
                        slotIndex,
                        expectedFoodTokenId,
                        foodItemView,
                        modelRemoved,
                        viewRemoved);

                    return false;
                }

                session.FridgeInventory.Store(
                    expectedFoodTokenId);

                foodItemView.Clear();

                UnityEngine.Object.Destroy(
                    foodItemView.gameObject);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                RollbackScoop(
                    session,
                    slotIndex,
                    expectedFoodTokenId,
                    foodItemView,
                    modelRemoved,
                    viewRemoved);

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
            if (session == null)
            {
                return;
            }

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
            if (session == null ||
                session.WaitingRack == null)
            {
                return;
            }

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
            if (_view == null)
            {
                return;
            }

            if (session?.FridgeInventory != null &&
                !session.FridgeInventory.IsEmpty)
            {
                _view.SetFullState();
            }
            else
            {
                _view.SetClosedState();
            }
        }

        private void DestroyTransientFood(
            FoodItemView foodItemView)
        {
            if (foodItemView != null)
            {
                _boardLayoutView
                    .DestroyTransientFoodItemView(
                        foodItemView);
            }
        }

        private bool CanContinue(
            GameplaySession session)
        {
            return session != null &&
                   _sessionGuard.IsCurrentSession(
                       session.SessionId) &&
                   session.CanContinueGameplay;
        }
    }
}
