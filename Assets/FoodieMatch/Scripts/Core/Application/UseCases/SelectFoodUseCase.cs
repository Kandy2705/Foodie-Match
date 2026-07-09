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
            IReadOnlyList<RequiredPackage> requiredPackages,
            WaitingRackState waitingRackState)
        {
            if (foodTokenId <= 0 ||
                requiredPackages == null ||
                waitingRackState == null)
            {
                return SelectFoodResult.InvalidSelection(foodTokenId);
            }

            if (_requiredPackageMatcher.TryFindBestMatchIndex(
                    requiredPackages,
                    foodTokenId,
                    out int requiredPackageIndex))
            {
                RequiredPackage requiredPackage =
                    requiredPackages[requiredPackageIndex];

                if (requiredPackage.TryPlaceFood(foodTokenId))
                {
                    return SelectFoodResult.PlacedInRequiredPackage(
                        foodTokenId,
                        requiredPackageIndex);
                }
            }

            if (waitingRackState.TryPlaceFood(
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
