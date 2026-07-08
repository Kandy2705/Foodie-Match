using FoodieMatch.Features.Food;

namespace FoodieMatch.Features.Board
{
    public readonly struct FoodSelectionContext
    {
        public FoodSelectionContext(FoodItemView foodItemView)
        {
            FoodItemView = foodItemView;
            FoodTokenId = foodItemView != null ? foodItemView.FoodTokenId : 0;
        }

        public FoodItemView FoodItemView { get; }
        public int FoodTokenId { get; }
    }
}
