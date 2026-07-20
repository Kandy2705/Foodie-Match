using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Motion;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class GrillCompletionCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayMotionPresenter _motionPresenter;
        private readonly BoardLayoutView _boardLayoutView;
        private readonly Dictionary<int, GameplaySession> _motionOwners = new();

        private GameplaySession _session;

        public event Action<GameplaySession> GrillCloseFinished;

        public GrillCompletionCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayMotionPresenter motionPresenter,
            BoardLayoutView boardLayoutView)
        {
            _sessionGuard = sessionGuard;
            _motionPresenter = motionPresenter;
            _boardLayoutView = boardLayoutView;
        }

        public void BeginSession(GameplaySession session)
        {
            _session = session;
            _motionOwners.Clear();
        }

        public void EndSession()
        {
            _session = null;
            _motionOwners.Clear();
        }

        public bool HasActiveMotion(GameplaySession session)
        {
            return IsCurrentSession(session) && _motionOwners.Count > 0;
        }

        public void TryCloseCompletedGrill(int grillPositionIndex, GameplaySession session)
        {
            if (!CanContinue(session) ||
                !session.Board.TryGetGrill(grillPositionIndex, out GrillModel grillModel) ||
                grillModel.HasRemainingFood ||
                _motionOwners.ContainsKey(grillPositionIndex))
            {
                return;
            }

            if (!_boardLayoutView.TryGetGrillView(grillPositionIndex, out GrillView grillView))
            {
                Debug.LogError($"Completed grill view {grillPositionIndex} could not be found.");
                return;
            }

            _motionOwners.Add(grillPositionIndex, session);
            _ = CloseGrillSafelyAsync(grillPositionIndex, grillView, session);
        }

        private async Task CloseGrillSafelyAsync(
            int grillPositionIndex,
            GrillView grillView,
            GameplaySession session)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await _motionPresenter.PlayGrillCloseLidAsync(grillView);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }
            finally
            {
                RemoveMotionOwner(grillPositionIndex, session);
            }

            if (!IsCurrentSession(session) || motionResult == MotionResult.Cancelled)
            {
                return;
            }

            if (motionResult == MotionResult.Failed)
            {
                Debug.LogError($"Grill {grillPositionIndex} close lid motion failed.");
            }

            GrillCloseFinished?.Invoke(session);
        }

        private void RemoveMotionOwner(int grillPositionIndex, GameplaySession expectedOwner)
        {
            if (_motionOwners.TryGetValue(grillPositionIndex, out GameplaySession owner) && owner == expectedOwner)
            {
                _motionOwners.Remove(grillPositionIndex);
            }
        }

        private bool CanContinue(GameplaySession session)
        {
            return IsCurrentSession(session) && session.CanContinueGameplay;
        }

        private bool IsCurrentSession(GameplaySession session)
        {
            return session != null &&
                   _session == session &&
                   _sessionGuard.IsCurrentSession(session.SessionId);
        }
    }
}
