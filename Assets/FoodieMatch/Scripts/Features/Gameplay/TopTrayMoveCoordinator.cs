using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class TopTrayMoveCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayMotionPresenter _motionPresenter;
        private readonly BoardLayoutView _boardLayoutView;

        public TopTrayMoveCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayMotionPresenter motionPresenter,
            BoardLayoutView boardLayoutView)
        {
            _sessionGuard = sessionGuard;
            _motionPresenter = motionPresenter;
            _boardLayoutView = boardLayoutView;
        }

        public void MoveFoodToGrill(int grillPositionIndex, GameplaySession session)
        {
            if (session == null ||
                !_sessionGuard.IsCurrentSession(session.SessionId) ||
                !session.Board.TryMoveTopTrayToGrill(grillPositionIndex, out GrillModel grillModel))
            {
                return;
            }

            if (!_boardLayoutView.TryPrepareTopTrayFoodMove(
                    grillModel, out IReadOnlyList<FoodItemView> foodItemViews,
                    out IReadOnlyList<Vector3> targetPositions))
            {
                Debug.LogError($"Could not prepare top tray move to grill {grillPositionIndex}.");
                return;
            }

            _ = MoveFoodSafelyAsync(grillModel, foodItemViews, targetPositions, session);
        }

        private async Task MoveFoodSafelyAsync(
            GrillModel grillModel,
            IReadOnlyList<FoodItemView> foodItemViews,
            IReadOnlyList<Vector3> targetPositions,
            GameplaySession session)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await _motionPresenter.MoveTopTrayFoodToGrillAsync(foodItemViews, targetPositions);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }

            if (!_sessionGuard.IsCurrentSession(session.SessionId))
            {
                return;
            }

            if (motionResult == MotionResult.Failed)
            {
                Debug.LogError($"Top tray food flight to grill {grillModel.PositionIndex} failed.");
            }

            if (!_boardLayoutView.CompleteTopTrayFoodMove(
                    grillModel, foodItemViews, session.CanSelectFood))
            {
                Debug.LogError($"Could not complete top tray move to grill {grillModel.PositionIndex}.");
            }
        }
    }
}
