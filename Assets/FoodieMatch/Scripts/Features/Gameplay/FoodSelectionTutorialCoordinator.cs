using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Features.Gameplay
{
    internal sealed class FoodSelectionTutorialCoordinator
    {
        private readonly List<FoodBoardAddress> _selectionSequence = new();

        private int _currentStepIndex;

        public bool IsActive => _currentStepIndex < _selectionSequence.Count;
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
        }

        public bool CanSelect(FoodBoardAddress address)
        {
            return !IsActive || CurrentTarget.Equals(address);
        }

        public bool MoveToNextStep(FoodBoardAddress selectedAddress)
        {
            if (!IsActive || !CurrentTarget.Equals(selectedAddress))
            {
                return false;
            }

            _currentStepIndex++;
            return true;
        }

        public void Stop()
        {
            _selectionSequence.Clear();
            _currentStepIndex = 0;
        }
    }
}
