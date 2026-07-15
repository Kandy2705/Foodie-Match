using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Core.Application.UseCases
{
    public sealed class RequiredPackageLifecycleUseCase
    {
        private readonly RequiredPackageGenerator _generator;
        private readonly RequiredPackageMatcher _matcher;

        public RequiredPackageLifecycleUseCase(
            RequiredPackageGenerator generator,
            RequiredPackageMatcher matcher)
        {
            _generator = generator;
            _matcher = matcher;
        }

        public bool TryCreateInitialPackages(
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageGenerationSettings settings,
            out RequiredPackageModel[] packages)
        {
            packages = null;

            if (board == null ||
                waitingRack == null ||
                settings == null)
            {
                return false;
            }

            RequiredPackageModel[] initialPackages =
                new RequiredPackageModel[
                    settings.InitialActivePackageCount];

            for (int i = 0; i < initialPackages.Length; i++)
            {
                if (!_generator.TryCreatePackage(
                        board,
                        waitingRack,
                        initialPackages,
                        settings,
                        out RequiredPackageModel package))
                {
                    return false;
                }

                initialPackages[i] = package;
            }

            packages = initialPackages;
            return true;
        }

        public bool TryReplaceCompletedPackage(
            int packageIndex,
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageModel[] packages,
            RequiredPackageGenerationSettings settings,
            out RequiredPackageModel newPackage)
        {
            newPackage = null;

            if (board == null ||
                waitingRack == null ||
                packages == null ||
                settings == null ||
                packageIndex < 0 ||
                packageIndex >= packages.Length ||
                packages[packageIndex] == null ||
                !packages[packageIndex].IsComplete)
            {
                return false;
            }

            if (_generator.TryCreatePackage(
                    board,
                    waitingRack,
                    packages,
                    settings,
                    out RequiredPackageModel generatedPackage))
            {
                newPackage = generatedPackage;
            }

            packages[packageIndex] = newPackage;
            return true;
        }

        public bool TryFindWaitingRackMatch(
            WaitingRackModel waitingRack,
            IReadOnlyList<RequiredPackageModel> packages,
            out WaitingRackTransfer transfer)
        {
            transfer = default;

            if (waitingRack == null || packages == null)
            {
                return false;
            }

            for (int rackSlotIndex = 0;
                 rackSlotIndex < waitingRack.Capacity;
                 rackSlotIndex++)
            {
                int foodTokenId =
                    waitingRack.GetFoodTokenIdAt(rackSlotIndex);

                if (!_matcher.TryFindBestMatchIndex(
                        packages,
                        foodTokenId,
                        out int packageIndex))
                {
                    continue;
                }

                transfer = new WaitingRackTransfer(
                    rackSlotIndex,
                    packageIndex,
                    foodTokenId);

                return true;
            }

            return false;
        }

        public bool TryMoveFoodFromWaitingRack(
            WaitingRackTransfer transfer,
            WaitingRackModel waitingRack,
            IReadOnlyList<RequiredPackageModel> packages)
        {
            if (waitingRack == null ||
                packages == null ||
                transfer.RackSlotIndex < 0 ||
                transfer.RackSlotIndex >= waitingRack.Capacity ||
                transfer.PackageIndex < 0 ||
                transfer.PackageIndex >= packages.Count ||
                transfer.FoodTokenId <= 0)
            {
                return false;
            }

            int foodTokenId = waitingRack.GetFoodTokenIdAt(
                transfer.RackSlotIndex);
            RequiredPackageModel package =
                packages[transfer.PackageIndex];

            if (foodTokenId != transfer.FoodTokenId ||
                package == null ||
                !package.CanAccept(foodTokenId))
            {
                return false;
            }

            if (!waitingRack.TryRemoveFoodAt(
                    transfer.RackSlotIndex,
                    out int removedFoodTokenId))
            {
                return false;
            }

            if (removedFoodTokenId == foodTokenId &&
                package.TryPlaceFood(foodTokenId))
            {
                return true;
            }

            waitingRack.TryRestoreFoodAt(
                transfer.RackSlotIndex,
                removedFoodTokenId);

            return false;
        }
    }
}
