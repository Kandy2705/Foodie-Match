using System;

namespace FoodieMatch.UI.Revive
{
    public sealed class RevivePopupViewActions
    {
        public RevivePopupViewActions(
            Action closeClicked,
            Action freeAdsClicked,
            Action playOnClicked)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
            FreeAdsClicked = freeAdsClicked ?? throw new ArgumentNullException(nameof(freeAdsClicked));
            PlayOnClicked = playOnClicked ?? throw new ArgumentNullException(nameof(playOnClicked));
        }

        public Action CloseClicked { get; }

        public Action FreeAdsClicked { get; }

        public Action PlayOnClicked { get; }
    }
}
