using System;

namespace FoodieMatch.UI.Heart
{
    public sealed class FillHeartPopupViewActions
    {
        public FillHeartPopupViewActions(
            Action closeClicked,
            Action freeAdsClicked,
            Action buyClicked,
            Action heartRecoveredToFull)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
            FreeAdsClicked = freeAdsClicked ?? throw new ArgumentNullException(nameof(freeAdsClicked));
            BuyClicked = buyClicked ?? throw new ArgumentNullException(nameof(buyClicked));
            HeartRecoveredToFull = heartRecoveredToFull ??
                throw new ArgumentNullException(nameof(heartRecoveredToFull));
        }

        public Action CloseClicked { get; }

        public Action FreeAdsClicked { get; }

        public Action BuyClicked { get; }

        public Action HeartRecoveredToFull { get; }
    }
}
