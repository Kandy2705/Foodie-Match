using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
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
            FoodBoardAddress address,
            BoardModel board,
            IReadOnlyList<RequiredPackageModel> requiredPackages,
            WaitingRackModel waitingRack)
        {
            if (board == null ||
                requiredPackages == null ||
                waitingRack == null)
            {
                return SelectFoodResult.InvalidSelection(
                    BoardRules.EmptyFoodTokenId);
            }

            int foodTokenId = board.GetFoodTokenId(address);

            if (!board.CanRemoveFood(address, foodTokenId))
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

                if (board.TryRemoveFood(address, foodTokenId) &&
                    requiredPackage.TryPlaceFood(foodTokenId))
                {
                    return SelectFoodResult.PlacedInRequiredPackage(
                        foodTokenId,
                        requiredPackageIndex);
                }

                board.TryRestoreFood(address, foodTokenId);
                return SelectFoodResult.InvalidSelection(foodTokenId);
            }

            if (!waitingRack.CanPlace(foodTokenId))
            {
                return SelectFoodResult.NoAvailablePlacement(foodTokenId);
            }

            if (board.TryRemoveFood(address, foodTokenId) &&
                waitingRack.TryPlaceFood(
                    foodTokenId,
                    out int waitingRackSlotIndex))
            {
                return SelectFoodResult.PlacedInWaitingRack(
                    foodTokenId,
                    waitingRackSlotIndex);
            }

            board.TryRestoreFood(address, foodTokenId);
            return SelectFoodResult.InvalidSelection(foodTokenId);
        }
    }
}
