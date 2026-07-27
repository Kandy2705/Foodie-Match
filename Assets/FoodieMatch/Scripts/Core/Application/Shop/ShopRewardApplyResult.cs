using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Shop
{
    public sealed class ShopRewardApplyResult
    {
        private readonly ReadOnlyDictionary<BoosterType, int> _boosterCounts;

        public ShopRewardApplyResult(
            long coinBalance,
            int heartCount,
            long unlimitedHeartEndUnixSeconds,
            bool adsRemoved,
            IReadOnlyDictionary<BoosterType, int> boosterCounts)
        {
            if (coinBalance < 0 || heartCount < 0 || unlimitedHeartEndUnixSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coinBalance));
            }

            if (boosterCounts == null)
            {
                throw new ArgumentNullException(nameof(boosterCounts));
            }

            _boosterCounts = new ReadOnlyDictionary<BoosterType, int>(
                new Dictionary<BoosterType, int>(boosterCounts));
            CoinBalance = coinBalance;
            HeartCount = heartCount;
            UnlimitedHeartEndUnixSeconds = unlimitedHeartEndUnixSeconds;
            AdsRemoved = adsRemoved;
        }

        public long CoinBalance { get; }

        public int HeartCount { get; }

        public long UnlimitedHeartEndUnixSeconds { get; }

        public bool AdsRemoved { get; }

        public IReadOnlyDictionary<BoosterType, int> BoosterCounts => _boosterCounts;
    }
}
