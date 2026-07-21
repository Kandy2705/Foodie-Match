using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Randomization;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSimulationCloner
    {
        private readonly BoardModelCloner _boardCloner = new();
        private readonly SelectFoodUseCase _selectFoodUseCase;
        private readonly LevelSimulationResolver _resolver;

        public LevelSimulationCloner(
            SelectFoodUseCase selectFoodUseCase,
            LevelSimulationResolver resolver)
        {
            _selectFoodUseCase = selectFoodUseCase;
            _resolver = resolver;
        }

        public LevelSimulation Clone(LevelSimulation source)
        {
            PackageRandomState randomState = source.PackageRandom.CaptureState();
            PackageRandom packageRandom = new(randomState.Seed);
            packageRandom.RestoreState(randomState);

            return new LevelSimulation(
                source.Level,
                _boardCloner.Clone(source.Board),
                CloneWaitingRack(source.WaitingRack),
                ClonePackages(source.RequiredPackages),
                packageRandom,
                source.ServedFoodCount,
                _selectFoodUseCase,
                _resolver);
        }

        private static WaitingRackModel CloneWaitingRack(WaitingRackModel source)
        {
            WaitingRackModel clone = new(source.Capacity);

            for (int slotIndex = 0; slotIndex < source.Capacity; slotIndex++)
            {
                int foodId = source.GetFoodTokenIdAt(slotIndex);

                if (foodId > 0 && !clone.TryRestoreFoodAt(slotIndex, foodId))
                {
                    throw new InvalidOperationException(
                        $"Waiting rack slot {slotIndex} could not be cloned.");
                }
            }

            return clone;
        }

        private static RequiredPackageModel[] ClonePackages(
            IReadOnlyList<RequiredPackageModel> source)
        {
            RequiredPackageModel[] clone = new RequiredPackageModel[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                RequiredPackageModel package = source[i];

                if (package != null)
                {
                    clone[i] = new RequiredPackageModel(
                        package.FoodTokenId,
                        package.RequiredAmount,
                        package.FilledAmount);
                }
            }

            return clone;
        }
    }
}
