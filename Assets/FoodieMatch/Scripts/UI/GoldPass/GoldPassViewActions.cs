using System;
using FoodieMatch.Core.Application.GoldPass;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassViewActions
    {
        public GoldPassViewActions(
            Action closeClicked,
            Action informationClicked,
            Action purchaseClicked,
            Action<int, GoldPassTrack> claimClicked,
            Action seasonExpired)
        {
            CloseClicked = closeClicked;
            InformationClicked = informationClicked;
            PurchaseClicked = purchaseClicked;
            ClaimClicked = claimClicked;
            SeasonExpired = seasonExpired;
        }

        public Action CloseClicked { get; }

        public Action InformationClicked { get; }

        public Action PurchaseClicked { get; }

        public Action<int, GoldPassTrack> ClaimClicked { get; }

        public Action SeasonExpired { get; }
    }
}
