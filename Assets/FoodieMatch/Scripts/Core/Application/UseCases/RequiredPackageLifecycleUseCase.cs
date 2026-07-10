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

        public RequiredPackageLifecycleResult ResolveCompletedPackage(
            int completedPackageIndex,
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageModel[] packages,
            RequiredPackageGenerationSettings settings)
        {
            RequiredPackageLifecycleResult result =
                new RequiredPackageLifecycleResult();

            if (board == null ||
                waitingRack == null ||
                packages == null ||
                settings == null ||
                completedPackageIndex < 0 ||
                completedPackageIndex >= packages.Length ||
                packages[completedPackageIndex] == null ||
                !packages[completedPackageIndex].IsComplete)
            {
                return result;
            }

            CompleteAndReplacePackage(
                completedPackageIndex,
                board,
                waitingRack,
                packages,
                settings,
                result);

            AutoFillWaitingRack(
                board,
                waitingRack,
                packages,
                settings,
                result);

            return result;
        }

        private void AutoFillWaitingRack(
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageModel[] packages,
            RequiredPackageGenerationSettings settings,
            RequiredPackageLifecycleResult result)
        {
            while (TryMoveFirstMatchingRackFood(
                       waitingRack,
                       packages,
                       out WaitingRackTransfer transfer))
            {
                result.AddTransfer(transfer);
                result.MarkPackageUpdated(transfer.PackageIndex);

                RequiredPackageModel package =
                    packages[transfer.PackageIndex];

                if (!package.IsComplete)
                {
                    continue;
                }

                CompleteAndReplacePackage(
                    transfer.PackageIndex,
                    board,
                    waitingRack,
                    packages,
                    settings,
                    result);
            }
        }

        private void CompleteAndReplacePackage(
            int packageIndex,
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageModel[] packages,
            RequiredPackageGenerationSettings settings,
            RequiredPackageLifecycleResult result)
        {
            result.AddCompletedPackage(packageIndex);

            if (_generator.TryCreatePackage(
                    board,
                    waitingRack,
                    packages,
                    settings,
                    out RequiredPackageModel nextPackage))
            {
                packages[packageIndex] = nextPackage;
            }
            else
            {
                packages[packageIndex] = null;
            }

            result.MarkPackageUpdated(packageIndex);
        }

        private bool TryMoveFirstMatchingRackFood(
            WaitingRackModel waitingRack,
            IReadOnlyList<RequiredPackageModel> packages,
            out WaitingRackTransfer transfer)
        {
            transfer = default;

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

                RequiredPackageModel package = packages[packageIndex];

                if (!waitingRack.TryRemoveFoodAt(
                        rackSlotIndex,
                        out int removedFoodTokenId))
                {
                    return false;
                }

                if (removedFoodTokenId != foodTokenId ||
                    !package.TryPlaceFood(foodTokenId))
                {
                    waitingRack.TryRestoreFoodAt(
                        rackSlotIndex,
                        removedFoodTokenId);

                    return false;
                }

                transfer = new WaitingRackTransfer(
                    rackSlotIndex,
                    packageIndex,
                    foodTokenId);

                return true;
            }

            return false;
        }
    }
}
