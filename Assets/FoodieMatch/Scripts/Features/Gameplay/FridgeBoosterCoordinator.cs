using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class FridgeBoosterCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly FridgeBoosterView _view;

        private bool _isRunning;

        public FridgeBoosterCoordinator(
            GameplaySessionGuard sessionGuard,
            FridgeBoosterView view)
        {
            _sessionGuard = sessionGuard;
            _view = view;
        }

        public void BeginSession()
        {
            _isRunning = false;
            _view?.HideImmediately();
        }

        public bool TryApply(GameplaySession session)
        {
            if (_isRunning ||
                _view == null ||
                !CanContinue(session) ||
                !session.IsInputEnabled)
            {
                return false;
            }

            _isRunning = true;
            session.DisableInput();

            _ = PlaySafelyAsync(session);
            return true;
        }

        public void EndSession()
        {
            _isRunning = false;

            if (_view == null)
            {
                return;
            }

            _view.CancelAnimations();
            _view.HideImmediately();
        }

        private async Task PlaySafelyAsync(
            GameplaySession session)
        {
            try
            {
                await _view.PlayEnterAndOpenAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isRunning = false;

                if (CanContinue(session))
                {
                    session.StartPlaying();
                }
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