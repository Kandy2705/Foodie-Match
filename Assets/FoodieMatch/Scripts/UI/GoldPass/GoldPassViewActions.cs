using System;
using FoodieMatch.Core.Application.GoldPass;
using FoodieMatch.UI.ClaimReward;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassViewActions
    {
        public GoldPassViewActions(
            Action closeClicked,
            Action informationClicked,
            Action purchaseClicked,
            Action lockedRewardClicked,
            Action<int, GoldPassTrack, ClaimRewardPopupData> claimClicked,
            Action<ClaimRewardPopupData> claimAllClicked,
            Action seasonExpired)
        {
            CloseClicked = closeClicked;
            InformationClicked = informationClicked;
            PurchaseClicked = purchaseClicked;
            LockedRewardClicked = lockedRewardClicked;
            ClaimClicked = claimClicked;
            ClaimAllClicked = claimAllClicked;
            SeasonExpired = seasonExpired;
        }

        public Action CloseClicked { get; }

        public Action InformationClicked { get; }

        public Action PurchaseClicked { get; }

        public Action LockedRewardClicked { get; }

        public Action<int, GoldPassTrack, ClaimRewardPopupData> ClaimClicked
        {
            get;
        }

        public Action<ClaimRewardPopupData> ClaimAllClicked { get; }

        public Action SeasonExpired { get; }
    }
}
