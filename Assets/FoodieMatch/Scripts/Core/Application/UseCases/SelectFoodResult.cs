namespace FoodieMatch.Core.Application.UseCases
{
    public readonly struct SelectFoodResult
    {
        public SelectFoodResult(
            SelectFoodResultType type,
            int foodTokenId,
            int targetIndex)
        {
            Type = type;
            FoodTokenId = foodTokenId;
            TargetIndex = targetIndex;
        }

        public SelectFoodResultType Type { get; }
        public int FoodTokenId { get; }
        public int TargetIndex { get; }

        public bool IsPlaced =>
            Type == SelectFoodResultType.PlacedInRequiredPackage ||
            Type == SelectFoodResultType.PlacedInWaitingRack;

        public static SelectFoodResult InvalidSelection(int foodTokenId)
        {
            return new SelectFoodResult(SelectFoodResultType.InvalidSelection, foodTokenId, -1);
        }

        public static SelectFoodResult PlacedInRequiredPackage(
            int foodTokenId,
            int requiredPackageIndex)
        {
            return new SelectFoodResult(
                SelectFoodResultType.PlacedInRequiredPackage,
                foodTokenId,
                requiredPackageIndex);
        }

        public static SelectFoodResult PlacedInWaitingRack(
            int foodTokenId,
            int waitingRackSlotIndex)
        {
            return new SelectFoodResult(
                SelectFoodResultType.PlacedInWaitingRack,
                foodTokenId,
                waitingRackSlotIndex);
        }

        public static SelectFoodResult WaitingRackFull(int foodTokenId)
        {
            return new SelectFoodResult(SelectFoodResultType.WaitingRackFull, foodTokenId, -1);
        }
    }
}
