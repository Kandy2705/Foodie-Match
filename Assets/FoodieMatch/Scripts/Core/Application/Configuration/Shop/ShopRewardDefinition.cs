using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.Shop
{
    public sealed class ShopRewardDefinition
    {
        private readonly ReadOnlyDictionary<BoosterType, int> _boosterAmounts;

        public ShopRewardDefinition(
            long coins,
            long unlimitedHeartSeconds,
            bool removeAds,
            IReadOnlyDictionary<BoosterType, int> boosterAmounts)
        {
            if (coins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coins));
            }

            if (unlimitedHeartSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unlimitedHeartSeconds));
            }

            if (boosterAmounts == null)
            {
                throw new ArgumentNullException(nameof(boosterAmounts));
            }

            Dictionary<BoosterType, int> copiedAmounts = new();

            foreach (KeyValuePair<BoosterType, int> amount in boosterAmounts)
            {
                if (!Enum.IsDefined(typeof(BoosterType), amount.Key))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(boosterAmounts));
                }

                if (amount.Value <= 0 || !copiedAmounts.TryAdd(amount.Key, amount.Value))
                {
                    throw new ArgumentException(
                        "Booster rewards must be unique positive amounts.",
                        nameof(boosterAmounts));
                }
            }

            Coins = coins;
            UnlimitedHeartSeconds = unlimitedHeartSeconds;
            RemoveAds = removeAds;
            _boosterAmounts = new ReadOnlyDictionary<BoosterType, int>(copiedAmounts);
        }

        public long Coins { get; }

        public long UnlimitedHeartSeconds { get; }

        public bool RemoveAds { get; }

        public IReadOnlyDictionary<BoosterType, int> BoosterAmounts => _boosterAmounts;
    }
}
