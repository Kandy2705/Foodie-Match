using System;
using System.Threading.Tasks;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using FoodieMatch.Features.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class WaitingRackPlacementCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayMotionPresenter _motionPresenter;
        private readonly WaitingRackView _waitingRackView;

        public WaitingRackPlacementCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayMotionPresenter motionPresenter,
            WaitingRackView waitingRackView)
        {
            _sessionGuard = sessionGuard;
            _motionPresenter = motionPresenter;
            _waitingRackView = waitingRackView;
        }

        public async Task<WaitingRackPlacementResult> PlaceFoodAsync(
            FoodItemView foodItemView,
            int rackSlotIndex,
            GameplaySession session)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await _motionPresenter.MoveFoodToWaitingRackAsync(
                    foodItemView,
                    rackSlotIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }

            if (!_sessionGuard.IsCurrentSession(session.SessionId))
            {
                return WaitingRackPlacementResult.Cancelled;
            }

            if (motionResult == MotionResult.Cancelled)
            {
                return WaitingRackPlacementResult.Cancelled;
            }

            if (motionResult == MotionResult.Completed)
            {
                return WaitingRackPlacementResult.Completed;
            }

            Debug.LogError($"Food flight to waiting rack slot {rackSlotIndex} failed.");

            if (ReconcilePlacement(rackSlotIndex, foodItemView))
            {
                return WaitingRackPlacementResult.Completed;
            }

            return WaitingRackPlacementResult.Failed;
        }

        private bool ReconcilePlacement(int rackSlotIndex, FoodItemView foodItemView)
        {
            if (_waitingRackView.CompleteFoodPlacementAt(rackSlotIndex, foodItemView))
            {
                return true;
            }

            return _waitingRackView.RestoreFoodAt(rackSlotIndex, foodItemView);
        }
    }
}
