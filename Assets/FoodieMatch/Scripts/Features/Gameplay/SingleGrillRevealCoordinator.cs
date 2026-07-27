using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.Food;
using FoodieMatch.Features.Motion;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class SingleGrillRevealCoordinator
    {
        private readonly GameplaySessionGuard _sessionGuard;
        private readonly GameplayAudioPresenter _audioPresenter;
        private readonly BoardLayoutView _boardLayoutView;

        public event Action<GameplaySession> RevealFailed;

        public SingleGrillRevealCoordinator(
            GameplaySessionGuard sessionGuard,
            GameplayAudioPresenter audioPresenter,
            BoardLayoutView boardLayoutView)
        {
            _sessionGuard = sessionGuard;
            _audioPresenter = audioPresenter;
            _boardLayoutView = boardLayoutView;
        }

        public void RevealNextFood(int grillPositionIndex, GameplaySession session)
        {
            if (!CanContinue(session) ||
                !session.Board.TryGetGrill(grillPositionIndex, out GrillModel grillModel) ||
                grillModel.Type != GrillType.Single ||
                !grillModel.CanMoveTopTrayToGrill())
            {
                return;
            }

            if (!session.Board.TryMoveTopTrayToGrill(grillPositionIndex, out grillModel))
            {
                return;
            }

            if (!_boardLayoutView.TryPrepareSingleGrillReveal(grillModel, out FoodItemView foodItemView))
            {
                Debug.LogError($"Could not prepare food reveal on single grill {grillPositionIndex}.");
                RevealFailed?.Invoke(session);
                return;
            }

            _ = RevealFoodSafelyAsync(grillModel, foodItemView, session);
        }

        private async Task RevealFoodSafelyAsync(
            GrillModel grillModel,
            FoodItemView foodItemView,
            GameplaySession session)
        {
            MotionResult motionResult;

            try
            {
                motionResult = await foodItemView.PlayRevealAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                motionResult = MotionResult.Failed;
            }

            if (!IsCurrentSession(session) || motionResult == MotionResult.Cancelled)
            {
                return;
            }

            if (!_boardLayoutView.CompleteSingleGrillReveal(
                    grillModel,
                    foodItemView,
                    session.CanSelectFood))
            {
                Debug.LogError($"Could not complete food reveal on single grill {grillModel.PositionIndex}.");
                RevealFailed?.Invoke(session);
                return;
            }

            if (motionResult == MotionResult.Completed)
            {
                foodItemView.PlayGrillSmoke();
                _audioPresenter.PlayFoodMovedToGrill();
            }
            else
            {
                Debug.LogError($"Food reveal on single grill {grillModel.PositionIndex} failed.");
            }
        }

        private bool CanContinue(GameplaySession session)
        {
            return IsCurrentSession(session) && session.CanContinueGameplay;
        }

        private bool IsCurrentSession(GameplaySession session)
        {
            return session != null && _sessionGuard.IsCurrentSession(session.SessionId);
        }
    }
}
