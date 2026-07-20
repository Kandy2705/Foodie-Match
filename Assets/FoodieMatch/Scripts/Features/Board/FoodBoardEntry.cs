using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Features.Food;

namespace FoodieMatch.Features.Board
{
    public readonly struct FoodBoardEntry
    {
        public FoodBoardEntry(
            FoodItemView foodItemView,
            FoodBoardAddress address)
        {
            FoodItemView = foodItemView;
            Address = address;
        }

        public FoodItemView FoodItemView { get; }
        public FoodBoardAddress Address { get; }
    }
}
