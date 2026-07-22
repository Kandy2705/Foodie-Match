namespace FoodieMatch.Core.Application.Advertising
{
    public interface IRewardedAdService
    {
        bool TryShow(
            RewardedAdPlacement placement,
            RewardedAdCallbacks callbacks);
    }
}
