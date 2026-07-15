using System;

namespace FoodieMatch.UI.BoosterGuide
{
    public sealed class BoosterGuidePopupViewActions
    {
        public BoosterGuidePopupViewActions(
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
