using System;
using FoodieMatch.Core.Application.Advertising;

namespace FoodieMatch.Infrastructure.Advertising
{
    internal static class LevelPlayAdPlacementNames
    {
        public static string GetName(RewardedAdPlacement placement)
        {
            return placement switch
            {
                RewardedAdPlacement.AddHeart => "Add_Heart",
                RewardedAdPlacement.BoosterBox => "Booster_Box",
                RewardedAdPlacement.BoosterFridge => "Booster_Fridge",
                RewardedAdPlacement.BoosterPlate => "Booster_Plate",
                RewardedAdPlacement.BoosterStorage => "Booster_Storage",
                RewardedAdPlacement.BoosterSwap => "Booster_Swap",
                RewardedAdPlacement.DoubleCoin => "Double_Coin",
                RewardedAdPlacement.DailyReward => "Daily_Reward",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(placement),
                    placement,
                    null)
            };
        }

        public static string GetName(InterstitialAdPlacement placement)
        {
            return placement switch
            {
                InterstitialAdPlacement.PostLevel => "Post_Level",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(placement),
                    placement,
                    null)
            };
        }
    }
}
