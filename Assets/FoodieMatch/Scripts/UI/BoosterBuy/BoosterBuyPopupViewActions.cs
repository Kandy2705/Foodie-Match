using System;

namespace FoodieMatch.UI.BoosterBuy
{
    public sealed class BoosterBuyPopupViewActions
    {
        public BoosterBuyPopupViewActions(
            Action closeClicked,
            Action freeAdsClicked,
            Action buyClicked)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
            FreeAdsClicked = freeAdsClicked ?? throw new ArgumentNullException(nameof(freeAdsClicked));
            BuyClicked = buyClicked ?? throw new ArgumentNullException(nameof(buyClicked));
        }

        public Action CloseClicked { get; }

        public Action FreeAdsClicked { get; }

        public Action BuyClicked { get; }
    }
}
