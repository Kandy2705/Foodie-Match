namespace FoodieMatch.Core.Application.Advertising
{
    public interface IInterstitialAdService
    {
        bool TryShow(
            InterstitialAdPlacement placement,
            InterstitialAdCallbacks callbacks);
    }
}
