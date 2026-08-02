using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class LevelTutorialDefinition
    {
        private readonly ReadOnlyCollection<FoodSelectionTutorialStep>
            _foodSelectionSequence;

        public LevelTutorialDefinition(
            IReadOnlyList<FoodSelectionTutorialStep> foodSelectionSequence)
        {
            if (foodSelectionSequence == null)
            {
                throw new ArgumentNullException(nameof(foodSelectionSequence));
            }

            if (foodSelectionSequence.Count == 0)
            {
                throw new ArgumentException(
                    "Food selection tutorial must contain at least one step.",
                    nameof(foodSelectionSequence));
            }

            HashSet<(int GrillId, int FoodSlotIndex)> foodAddresses = new();
            List<FoodSelectionTutorialStep> copiedSequence = new(foodSelectionSequence.Count);

            for (int i = 0; i < foodSelectionSequence.Count; i++)
            {
                FoodSelectionTutorialStep step = foodSelectionSequence[i];

                if (!foodAddresses.Add((step.GrillId, step.FoodSlotIndex)))
                {
                    throw new ArgumentException(
                        "Food selection tutorial cannot contain duplicate food addresses.",
                        nameof(foodSelectionSequence));
                }

                copiedSequence.Add(step);
            }

            _foodSelectionSequence = copiedSequence.AsReadOnly();
        }

        public IReadOnlyList<FoodSelectionTutorialStep> FoodSelectionSequence =>
            _foodSelectionSequence;
    }
}
