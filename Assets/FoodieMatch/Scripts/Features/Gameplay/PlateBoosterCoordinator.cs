using System;
using System.Threading.Tasks;
using FoodieMatch.Features.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class PlateBoosterCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly WaitingRackView _waitingRackView;

        public PlateBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            WaitingRackView waitingRackView)
        {
            _sessionGuard = sessionGuard;
            _waitingRackView = waitingRackView;
        }

        public bool TryApply(GameplaySession session)
        {
            if (!CanApply(session))
            {
                return false;
            }

            if (!session.WaitingRack.TryExpandBy(1))
            {
                return false;
            }

            _ = PlayMotionSafelyAsync();
            return true;
        }

        private async Task PlayMotionSafelyAsync()
        {
            try
            {
                await _waitingRackView.PlayAddSlotAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private bool CanApply(GameplaySession session)
        {
            return session != null &&
                   session.CanContinueGameplay &&
                   session.IsInputEnabled &&
                   session.WaitingRack != null &&
                   _waitingRackView != null &&
                   _waitingRackView.CanAddSlot() &&
                   _sessionGuard.IsCurrentSession(session.SessionId);
        }
    }
}
