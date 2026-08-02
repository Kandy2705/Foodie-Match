using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class FoodSelectionTutorialCoordinator
    {
        private enum TutorialState
        {
            Inactive,
            WaitingForSelection,
            MovingToTarget
        }

        private readonly List<FoodBoardAddress> _selectionSequence = new();

        private int _currentStepIndex;
        private TutorialState _state;

        public bool IsActive => _state != TutorialState.Inactive;
        public FoodBoardAddress CurrentTarget => _selectionSequence[_currentStepIndex];

        public void Start(LevelTutorialDefinition tutorial, BoardModel board)
        {
            Stop();

            if (tutorial == null)
            {
                return;
            }

            for (int i = 0; i < tutorial.FoodSelectionSequence.Count; i++)
            {
                FoodSelectionTutorialStep step = tutorial.FoodSelectionSequence[i];

                if (!board.TryGetGrillById(step.GrillId, out GrillModel grill))
                {
                    throw new InvalidOperationException(
                        $"Tutorial grill {step.GrillId} does not exist in the board.");
                }

                _selectionSequence.Add(
                    new FoodBoardAddress(
                        grill.PositionIndex,
                        step.FoodSlotIndex));
            }

            _state = TutorialState.WaitingForSelection;
        }

        public bool CanSelect(FoodBoardAddress address)
        {
            return !IsActive ||
                   _state == TutorialState.WaitingForSelection &&
                   CurrentTarget.Equals(address);
        }

        public bool TryAdvanceAfterSelection(FoodBoardAddress selectedAddress)
        {
            if (_state != TutorialState.WaitingForSelection ||
                !CurrentTarget.Equals(selectedAddress))
            {
                return false;
            }

            _currentStepIndex++;

            _state = _currentStepIndex < _selectionSequence.Count
                ? TutorialState.MovingToTarget
                : TutorialState.Inactive;
            return true;
        }

        public bool CompleteTargetMove(FoodBoardAddress targetAddress)
        {
            if (_state != TutorialState.MovingToTarget ||
                !CurrentTarget.Equals(targetAddress))
            {
                return false;
            }

            _state = TutorialState.WaitingForSelection;
            return true;
        }

        public void Stop()
        {
            _selectionSequence.Clear();
            _currentStepIndex = 0;
            _state = TutorialState.Inactive;
        }
    }
}
