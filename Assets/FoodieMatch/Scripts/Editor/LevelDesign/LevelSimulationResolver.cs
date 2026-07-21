using System.Collections.Generic;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.RequiredPackage;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSimulationResolver
    {
        private readonly RequiredPackageLifecycleUseCase _packageLifecycleUseCase;

        public LevelSimulationResolver(
            RequiredPackageLifecycleUseCase packageLifecycleUseCase)
        {
            _packageLifecycleUseCase = packageLifecycleUseCase;
        }

        public bool TryResolve(LevelSimulation simulation)
        {
            while (true)
            {
                if (!TryReplaceCompletedPackages(simulation, out bool replacedPackage) ||
                    !TryMoveWaitingRackFood(simulation, out bool movedFood))
                {
                    return false;
                }

                if (!replacedPackage && !movedFood)
                {
                    return true;
                }
            }
        }

        private bool TryReplaceCompletedPackages(
            LevelSimulation simulation,
            out bool replacedPackage)
        {
            replacedPackage = false;
            RequiredPackageModel[] packages = simulation.RequiredPackages;

            for (int packageIndex = 0; packageIndex < packages.Length; packageIndex++)
            {
                RequiredPackageModel package = packages[packageIndex];

                if (package == null || !package.IsComplete)
                {
                    continue;
                }

                IReadOnlyList<RequiredPackageModel> reservations = packages;
                float progressRatio =
                    (float)simulation.ServedFoodCount / simulation.Level.TotalFoodCount;

                if (!_packageLifecycleUseCase.TryPrepareReplacementPackage(
                        packageIndex,
                        simulation.Board,
                        simulation.WaitingRack,
                        packages,
                        reservations,
                        simulation.Level.PackageSelectionSettings,
                        progressRatio,
                        simulation.PackageRandom,
                        out RequiredPackageModel replacementPackage) ||
                    !_packageLifecycleUseCase.TryPublishReplacementPackage(
                        packageIndex,
                        package,
                        replacementPackage,
                        packages))
                {
                    return false;
                }

                replacedPackage = true;
            }

            return true;
        }

        private bool TryMoveWaitingRackFood(
            LevelSimulation simulation,
            out bool movedFood)
        {
            movedFood = false;

            while (_packageLifecycleUseCase.TryFindWaitingRackMatch(
                       simulation.WaitingRack,
                       simulation.RequiredPackages,
                       out WaitingRackTransfer transfer))
            {
                if (!_packageLifecycleUseCase.TryMoveFoodFromWaitingRack(
                        transfer,
                        simulation.WaitingRack,
                        simulation.RequiredPackages) ||
                    !simulation.TryServeFood())
                {
                    return false;
                }

                movedFood = true;
            }

            return true;
        }
    }
}
