using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public sealed class GoldPassRewardDefinition
    {
        private readonly ReadOnlyCollection<GoldPassRewardDefinition> _contents;

        private GoldPassRewardDefinition(
            GoldPassRewardType type,
            long amount,
            long unlimitedHeartSeconds,
            BoosterType? boosterType,
            IReadOnlyList<GoldPassRewardDefinition> contents)
        {
            Type = type;
            Amount = amount;
            UnlimitedHeartSeconds = unlimitedHeartSeconds;
            BoosterType = boosterType;
            _contents = new ReadOnlyCollection<GoldPassRewardDefinition>(
                contents == null
                    ? new List<GoldPassRewardDefinition>()
                    : new List<GoldPassRewardDefinition>(contents));
        }

        public GoldPassRewardType Type { get; }

        public long Amount { get; }

        public long UnlimitedHeartSeconds { get; }

        public BoosterType? BoosterType { get; }

        public IReadOnlyList<GoldPassRewardDefinition> Contents => _contents;

        public static GoldPassRewardDefinition CreateCoin(long amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return new GoldPassRewardDefinition(
                GoldPassRewardType.Coin,
                amount,
                0,
                null,
                null);
        }

        public static GoldPassRewardDefinition CreateUnlimitedHeart(
            long durationSeconds)
        {
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            return new GoldPassRewardDefinition(
                GoldPassRewardType.UnlimitedHeart,
                0,
                durationSeconds,
                null,
                null);
        }

        public static GoldPassRewardDefinition CreateBooster(
            BoosterType boosterType,
            int amount)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType) ||
                boosterType ==
                FoodieMatch.Core.Domain.Booster.BoosterType.Box)
            {
                throw new ArgumentOutOfRangeException(nameof(boosterType));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return new GoldPassRewardDefinition(
                GoldPassRewardType.Booster,
                amount,
                0,
                boosterType,
                null);
        }

        public static GoldPassRewardDefinition CreateTreasure(
            GoldPassRewardType type,
            IReadOnlyList<GoldPassRewardDefinition> contents)
        {
            if (type != GoldPassRewardType.Treasure1 &&
                type != GoldPassRewardType.Treasure2 &&
                type != GoldPassRewardType.Treasure3)
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (contents == null || contents.Count == 0 || contents.Count > 5)
            {
                throw new ArgumentException(
                    "Treasure must contain between one and five rewards.",
                    nameof(contents));
            }

            for (int i = 0; i < contents.Count; i++)
            {
                GoldPassRewardDefinition content = contents[i];

                if (content == null || content.IsTreasure)
                {
                    throw new ArgumentException(
                        "Treasure contents must be non-treasure rewards.",
                        nameof(contents));
                }
            }

            return new GoldPassRewardDefinition(type, 0, 0, null, contents);
        }

        public bool IsTreasure =>
            Type == GoldPassRewardType.Treasure1 ||
            Type == GoldPassRewardType.Treasure2 ||
            Type == GoldPassRewardType.Treasure3;
    }
}
