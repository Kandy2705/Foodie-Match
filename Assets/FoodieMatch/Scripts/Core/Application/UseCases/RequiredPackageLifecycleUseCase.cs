using System.Collections.Generic;
using FoodieMatch.Core.Application.Randomization;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.Fridge;
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
            PackageSelectionSettings settings,
            PackageRandom random,
            out RequiredPackageModel[] packages)
        {
            packages = null;

            if (board == null ||
                waitingRack == null ||
                settings == null ||
                random == null)
            {
                return false;
            }

            RequiredPackageModel[] initialPackages =
                new RequiredPackageModel[LevelRules.ActivePackageCount];

            for (int i = 0; i < initialPackages.Length; i++)
            {
                if (!_generator.TryCreatePackage(
                        board,
                        waitingRack,
                        initialPackages,
                        settings.EarlyWeights,
                        random,
                        out RequiredPackageModel package))
                {
                    return false;
                }

                initialPackages[i] = package;
            }

            packages = initialPackages;
            return true;
        }

        public bool TryPrepareReplacementPackage(
            int packageIndex,
            BoardModel board,
            WaitingRackModel waitingRack,
            RequiredPackageModel[] packages,
            IReadOnlyList<RequiredPackageModel> packageReservations,
            PackageSelectionSettings settings,
            float progressRatio,
            PackageRandom random,
            out RequiredPackageModel replacementPackage)
        {
            replacementPackage = null;

            if (board == null ||
                waitingRack == null ||
                packages == null ||
                packageReservations == null ||
                packageReservations.Count != packages.Length ||
                settings == null ||
                random == null ||
                float.IsNaN(progressRatio) ||
                float.IsInfinity(progressRatio) ||
                progressRatio < 0f ||
                progressRatio > 1f ||
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
                    packageReservations,
                    settings.GetWeights(progressRatio),
                    random,
                    out RequiredPackageModel generatedPackage))
            {
                replacementPackage = generatedPackage;
            }

            return true;
        }

        public bool TryPublishReplacementPackage(
            int packageIndex,
            RequiredPackageModel expectedPackage,
            RequiredPackageModel replacementPackage,
            RequiredPackageModel[] packages)
        {
            if (packages == null ||
                packageIndex < 0 ||
                packageIndex >= packages.Length ||
                expectedPackage == null ||
                !expectedPackage.IsComplete ||
                packages[packageIndex] != expectedPackage)
            {
                return false;
            }

            packages[packageIndex] = replacementPackage;
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

        public bool TryFindFridgeMatch(
            FridgeInventoryModel fridge,
            IReadOnlyList<RequiredPackageModel> packages,
            out FridgeTransfer transfer)
        {
            transfer = default;

            if (fridge == null ||
                fridge.IsEmpty ||
                packages == null)
            {
                return false;
            }

            IReadOnlyList<int> foodTokenIds =
                fridge.GetAllTokenIds();

            for (int i = 0;
                 i < foodTokenIds.Count;
                 i++)
            {
                int foodTokenId =
                    foodTokenIds[i];

                if (!_matcher.TryFindBestMatchIndex(
                        packages,
                        foodTokenId,
                        out int packageIndex))
                {
                    continue;
                }

                transfer = new FridgeTransfer(
                    packageIndex,
                    foodTokenId);

                return true;
            }

            return false;
        }

        public bool TryMoveFoodFromFridge(
            FridgeTransfer transfer,
            FridgeInventoryModel fridge,
            IReadOnlyList<RequiredPackageModel> packages)
        {
            if (fridge == null ||
                packages == null ||
                transfer.PackageIndex < 0 ||
                transfer.PackageIndex >= packages.Count ||
                transfer.FoodTokenId <= 0)
            {
                return false;
            }

            RequiredPackageModel package =
                packages[transfer.PackageIndex];

            if (package == null ||
                !package.CanAccept(transfer.FoodTokenId))
            {
                return false;
            }

            // Lấy token khỏi tủ.
            if (!fridge.TryTake(
                    transfer.FoodTokenId,
                    out int takenFoodTokenId))
            {
                return false;
            }

            // Đưa token vào order.
            if (takenFoodTokenId ==
                    transfer.FoodTokenId &&
                package.TryPlaceFood(takenFoodTokenId))
            {
                return true;
            }

            // Order nhận thất bại thì trả token lại tủ.
            fridge.Restore(takenFoodTokenId);
            return false;
        }
    }
}
