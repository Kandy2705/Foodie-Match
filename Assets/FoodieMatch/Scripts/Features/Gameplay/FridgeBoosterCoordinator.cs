using System;
using System.Threading.Tasks;
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

        private GameplaySession _activeSession;
        private bool _isRunning;

        public FridgeBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            FridgeBoosterView view,
            WaitingRackView waitingRackView)
        {
            _sessionGuard = sessionGuard;
            _view = view;
            _waitingRackView = waitingRackView;
        }

        public void BeginSession()
        {
            _isRunning = false;
            _activeSession = null;
            _view?.HideImmediately();
        }

        public bool TryApply(GameplaySession session)
        {
            if (_isRunning ||
                _view == null ||
                _waitingRackView == null ||
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

            if (!session.TryActivateFridgeInventory(out _))
            {
                Debug.Log(
                    "Fridge inventory could not be activated.");

                return false;
            }

            _activeSession = session;
            _isRunning = true;

            session.DisableInput();

            _ = PlaySafelyAsync(session);
            return true;
        }

        public void EndSession()
        {
            GameplaySession session = _activeSession;

            _isRunning = false;
            _activeSession = null;

            _view?.CancelAnimations();
            _view?.HideImmediately();

            session?.ClearFridgeInventory();
        }

        private async Task PlaySafelyAsync(
            GameplaySession session)
        {
            try
            {
                await PlayAsync(session);
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
                    _isRunning = false;

                    if (CanContinue(session))
                    {
                        session.StartPlaying();
                    }
                }
            }
        }

        private async Task PlayAsync(
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
                        $"Fridge stopped because slot " +
                        $"{slotIndex} could not be scooped.");

                    break;
                }
            }

            if (!CanContinue(session))
            {
                return;
            }

            if (session.FridgeInventory != null &&
                session.FridgeInventory.Count > 0)
            {
                _view.SetFullState();
            }
            else
            {
                _view.SetClosedState();
            }

            await _view.PlaySpoonExitLeftAsync();

            Debug.Log(
                $"Fridge scoop completed. Stored food: " +
                $"{session.FridgeInventory?.Count ?? 0}");
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
                    $"Fridge could not find visual food " +
                    $"at Waiting Rack slot {slotIndex}.");

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
                if (!session.WaitingRack.TryRemoveFoodAt(
                        slotIndex,
                        out int removedTokenId) ||
                    removedTokenId != expectedFoodTokenId)
                {
                    Debug.LogError(
                        $"Fridge could not remove model token " +
                        $"at slot {slotIndex}.");

                    return false;
                }

                modelRemoved = true;

                FoodItemView removedView =
                    _waitingRackView.RemoveFoodAt(
                        slotIndex);

                if (removedView == null ||
                    removedView != foodItemView)
                {
                    Debug.LogError(
                        $"Fridge could not remove visual food " +
                        $"at slot {slotIndex}.");

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
                bool restored =
                    _waitingRackView.RestoreFoodAt(
                        slotIndex,
                        foodItemView);

                if (!restored)
                {
                    Debug.LogError(
                        $"Fridge failed to restore visual food " +
                        $"at slot {slotIndex}.");
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

            if (!session.WaitingRack.TryRestoreFoodAt(
                    slotIndex,
                    foodTokenId))
            {
                Debug.LogError(
                    $"Fridge failed to restore model food " +
                    $"at slot {slotIndex}.");
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