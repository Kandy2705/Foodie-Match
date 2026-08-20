using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Domain.Booster;
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
            return CreateClaimPopupData(rewards, visualCatalog);
        }

        public static ClaimRewardPopupData CreateAggregatedClaimPopupData(
            IReadOnlyList<GoldPassRewardDefinition> rewards,
            GoldPassRewardVisualCatalogSO visualCatalog)
        {
            long coinAmount = 0;
            long unlimitedHeartSeconds = 0;
            Dictionary<BoosterType, int> boosterAmounts = new();

            for (int i = 0; i < rewards.Count; i++)
            {
                CollectReward(
                    rewards[i],
                    ref coinAmount,
                    ref unlimitedHeartSeconds,
                    boosterAmounts);
            }

            List<GoldPassRewardDefinition> aggregatedRewards = new();

            if (coinAmount > 0)
            {
                aggregatedRewards.Add(
                    GoldPassRewardDefinition.CreateCoin(coinAmount));
            }

            if (unlimitedHeartSeconds > 0)
            {
                aggregatedRewards.Add(
                    GoldPassRewardDefinition.CreateUnlimitedHeart(
                        unlimitedHeartSeconds));
            }

            AddBoosterReward(
                aggregatedRewards,
                boosterAmounts,
                BoosterType.Plate);
            AddBoosterReward(
                aggregatedRewards,
                boosterAmounts,
                BoosterType.Storage);
            AddBoosterReward(
                aggregatedRewards,
                boosterAmounts,
                BoosterType.Swap);
            AddBoosterReward(
                aggregatedRewards,
                boosterAmounts,
                BoosterType.Fridge);

            return CreateClaimPopupData(aggregatedRewards, visualCatalog);
        }

        private static ClaimRewardPopupData CreateClaimPopupData(
            IReadOnlyList<GoldPassRewardDefinition> rewards,
            GoldPassRewardVisualCatalogSO visualCatalog)
        {
            List<ClaimRewardItemData> items = new(rewards.Count);

            for (int i = 0; i < rewards.Count; i++)
            {
                GoldPassRewardDefinition reward = rewards[i];
                items.Add(
                    new ClaimRewardItemData(
                        visualCatalog.GetIcon(reward),
                        GetAmountText(reward)));
            }

            return new ClaimRewardPopupData(
                ClaimRewardTitle.GoldPass,
                items);
        }

        private static void CollectReward(
            GoldPassRewardDefinition reward,
            ref long coinAmount,
            ref long unlimitedHeartSeconds,
            Dictionary<BoosterType, int> boosterAmounts)
        {
            if (reward.IsTreasure)
            {
                for (int i = 0; i < reward.Contents.Count; i++)
                {
                    CollectReward(
                        reward.Contents[i],
                        ref coinAmount,
                        ref unlimitedHeartSeconds,
                        boosterAmounts);
                }

                return;
            }

            switch (reward.Type)
            {
                case GoldPassRewardType.Coin:
                    coinAmount = checked(coinAmount + reward.Amount);
                    return;
                case GoldPassRewardType.UnlimitedHeart:
                    unlimitedHeartSeconds = checked(
                        unlimitedHeartSeconds +
                        reward.UnlimitedHeartSeconds);
                    return;
                case GoldPassRewardType.Booster:
                    BoosterType boosterType = reward.BoosterType.Value;
                    int currentAmount = boosterAmounts.TryGetValue(
                        boosterType,
                        out int amount)
                            ? amount
                            : 0;
                    boosterAmounts[boosterType] = checked(
                        currentAmount + (int)reward.Amount);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reward));
            }
        }

        private static void AddBoosterReward(
            List<GoldPassRewardDefinition> rewards,
            Dictionary<BoosterType, int> boosterAmounts,
            BoosterType boosterType)
        {
            if (boosterAmounts.TryGetValue(boosterType, out int amount))
            {
                rewards.Add(
                    GoldPassRewardDefinition.CreateBooster(
                        boosterType,
                        amount));
            }
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
