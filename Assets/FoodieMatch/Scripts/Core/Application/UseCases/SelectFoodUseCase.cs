using System.Collections.Generic;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;

namespace FoodieMatch.Core.Application.UseCases
{
    public sealed class SelectFoodUseCase
    {
        private readonly RequiredPackageMatcher _requiredPackageMatcher;

        public SelectFoodUseCase(
            RequiredPackageMatcher requiredPackageMatcher)
        {
            _requiredPackageMatcher = requiredPackageMatcher;
        }

        public SelectFoodResult Execute(
            int foodTokenId,
            IReadOnlyList<RequiredPackageModel> requiredPackages,
            WaitingRackModel waitingRack)
        {
            if (foodTokenId <= 0 ||
                requiredPackages == null ||
                waitingRack == null)
            {
                return SelectFoodResult.InvalidSelection(foodTokenId);
            }

            if (_requiredPackageMatcher.TryFindBestMatchIndex(
                    requiredPackages,
                    foodTokenId,
                    out int requiredPackageIndex))
            {
                RequiredPackageModel requiredPackage =
                    requiredPackages[requiredPackageIndex];

                if (requiredPackage.TryPlaceFood(foodTokenId))
                {
                    return SelectFoodResult.PlacedInRequiredPackage(
                        foodTokenId,
                        requiredPackageIndex);
                }
            }

            if (waitingRack.TryPlaceFood(
                    foodTokenId,
                    out int waitingRackSlotIndex))
            {
                return SelectFoodResult.PlacedInWaitingRack(
                    foodTokenId,
                    waitingRackSlotIndex);
            }

            return SelectFoodResult.NoAvailablePlacement(foodTokenId);
        }
    }
}
