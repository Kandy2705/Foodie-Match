namespace FoodieMatch.Core.Application.UseCases
{
    public enum SelectFoodResultType
    {
        None = 0,
        PlacedInRequiredPackage = 1,
        PlacedInWaitingRack = 2,
        WaitingRackFull = 3,
        InvalidSelection = 4
    }
}
