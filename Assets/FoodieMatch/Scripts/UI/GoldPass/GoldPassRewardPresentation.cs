using System;
using FoodieMatch.Core.Application.Configuration.GoldPass;

namespace FoodieMatch.UI.GoldPass
{
    internal static class GoldPassRewardPresentation
    {
        public static string GetAmountText(GoldPassRewardDefinition reward)
        {
            switch (reward.Type)
            {
                case GoldPassRewardType.Coin:
                case GoldPassRewardType.Booster:
                    return $"x{reward.Amount}";
                case GoldPassRewardType.UnlimitedHeart:
                    return $"{reward.UnlimitedHeartSeconds / 60}m";
                case GoldPassRewardType.Treasure1:
                case GoldPassRewardType.Treasure2:
                case GoldPassRewardType.Treasure3:
                    return string.Empty;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reward));
            }
        }
    }
}
