using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Features.Food;

namespace FoodieMatch.Features.Gameplay
{
    internal readonly struct PackageFlight
    {
        public PackageFlight(
            FoodItemView foodItemView,
            RequiredPackageModel expectedPackage,
            int packageIndex,
            int requiredAmount,
            int filledSlotIndex)
        {
            FoodItemView = foodItemView;
            ExpectedPackage = expectedPackage;
            PackageIndex = packageIndex;
            RequiredAmount = requiredAmount;
            FilledSlotIndex = filledSlotIndex;
        }

        public FoodItemView FoodItemView { get; }
        public RequiredPackageModel ExpectedPackage { get; }
        public int PackageIndex { get; }
        public int RequiredAmount { get; }
        public int FilledSlotIndex { get; }
    }
}
