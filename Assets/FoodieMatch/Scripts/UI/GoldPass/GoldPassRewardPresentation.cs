using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.UI.ClaimReward;

namespace FoodieMatch.UI.GoldPass
{
    internal static class GoldPassRewardPresentation
    {
        public static ClaimRewardPopupData CreateClaimPopupData(
            GoldPassRewardDefinition reward,
            GoldPassRewardVisualCatalogSO visualCatalog)
        {
            IReadOnlyList<GoldPassRewardDefinition> rewards = reward.IsTreasure
                ? reward.Contents
                : new[] { reward };
            List<ClaimRewardItemData> items = new(rewards.Count);

            for (int i = 0; i < rewards.Count; i++)
            {
                GoldPassRewardDefinition item = rewards[i];
                items.Add(
                    new ClaimRewardItemData(
                        visualCatalog.GetIcon(item),
                        GetAmountText(item)));
            }

            return new ClaimRewardPopupData(
                ClaimRewardTitle.GoldPass,
                items);
        }

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
