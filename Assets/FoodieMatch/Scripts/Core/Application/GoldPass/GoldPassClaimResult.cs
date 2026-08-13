namespace FoodieMatch.Core.Application.GoldPass
{
    public enum GoldPassClaimResult
    {
        Succeeded = 0,
        MilestoneNotFound = 1,
        MilestoneLocked = 2,
        SeasonPassRequired = 3,
        AlreadyClaimed = 4
    }
}
