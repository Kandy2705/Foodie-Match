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

            if (!_boardLayoutView.TryPrepareTopTrayFoodMove(grillModel, out TopTrayMoveVisuals moveVisuals))
            {
                Debug.LogError($"Could not prepare top tray move to grill {grillPositionIndex}.");
                return;
            }

            _ = MoveFoodSafelyAsync(grillModel, moveVisuals, session);
        }

        private async Task MoveFoodSafelyAsync(
            GrillModel grillModel,
            TopTrayMoveVisuals moveVisuals,
            GameplaySession session)
        {
            List<Task> motionTasks = new();
            int flightOrder = 0;
            float transitionDuration = 0f;
            float startInterval = _motionPresenter.TopTrayFlightStartInterval;

            if (!IsValidTime(startInterval))
            {
                Debug.LogError("Top tray flight start interval is invalid.");
                startInterval = 0f;
            }

            for (int foodItemIndex = 0; foodItemIndex < moveVisuals.MovingFoodItems.Count; foodItemIndex++)
            {
                FoodItemView foodItemView = moveVisuals.MovingFoodItems[foodItemIndex];

                if (foodItemView == null)
                {
                    continue;
                }

                float startDelay = flightOrder * startInterval;
                float flightDuration = IsValidTime(foodItemView.TopTrayToGrillFlightDuration)
                    ? foodItemView.TopTrayToGrillFlightDuration
                    : 0f;

                transitionDuration = Mathf.Max(transitionDuration, startDelay + flightDuration);
                motionTasks.Add(MoveFoodItemSafelyAsync(
                    grillModel,
                    moveVisuals,
                    foodItemView,
                    foodItemIndex,
                    startDelay,
                    session));
                flightOrder++;
            }

            motionTasks.Add(PlayFadeTransitionSafelyAsync(
                grillModel,
                moveVisuals,
                transitionDuration,
                session));

            await Task.WhenAll(motionTasks);
        }

        private async Task MoveFoodItemSafelyAsync(
            GrillModel grillModel,
            TopTrayMoveVisuals moveVisuals,
            FoodItemView foodItemView,
            int foodItemIndex,
            float startDelay,
            GameplaySession session)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await _motionPresenter.MoveTopTrayFoodToGrillAsync(
                    foodItemView,
                    moveVisuals.TargetPositions[foodItemIndex],
                    startDelay);
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
                Debug.LogError(
                    $"Top tray food {foodItemIndex} flight to grill " +
                    $"{grillModel.PositionIndex} failed.");
            }

            if (!_boardLayoutView.CompleteTopTrayFoodMoveAt(
                    grillModel,
                    moveVisuals,
                    foodItemIndex,
                    session.CanSelectFood))
            {
                Debug.LogError(
                    $"Could not complete top tray food {foodItemIndex} move " +
                    $"to grill {grillModel.PositionIndex}.");
            }
        }

        private async Task PlayFadeTransitionSafelyAsync(
            GrillModel grillModel,
            TopTrayMoveVisuals moveVisuals,
            float duration,
            GameplaySession session)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await _motionPresenter.PlayTopTrayFadeTransitionAsync(
                    moveVisuals.DepartingTray,
                    moveVisuals.NewTopTrayFoodItems,
                    duration);
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
                Debug.LogError($"Top tray fade transition on grill {grillModel.PositionIndex} failed.");
            }

            if (!_boardLayoutView.CompleteTopTrayTransition(grillModel, moveVisuals))
            {
                Debug.LogError($"Could not complete top tray transition on grill {grillModel.PositionIndex}.");
            }
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
