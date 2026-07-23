using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.Economy
{
    public sealed class GameEconomyConfigSnapshot : IGameEconomyConfig
    {
        private static readonly BoosterType[] AllBoosterTypes =
            (BoosterType[])Enum.GetValues(typeof(BoosterType));

        private readonly ReadOnlyDictionary<BoosterType, int> _boosterPrices;

        public GameEconomyConfigSnapshot(
            int levelCompleteCoinReward,
            int rewardedAdCoinMultiplier,
            int coinValuePerRewardImage,
            IReadOnlyDictionary<BoosterType, int> boosterPrices)
        {
            ValidatePositiveValue(
                levelCompleteCoinReward,
                nameof(levelCompleteCoinReward));

            if (rewardedAdCoinMultiplier <= 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rewardedAdCoinMultiplier),
                    rewardedAdCoinMultiplier,
                    "Rewarded ad coin multiplier must be greater than one.");
            }

            ValidatePositiveValue(
                coinValuePerRewardImage,
                nameof(coinValuePerRewardImage));

            LevelCompleteCoinReward = levelCompleteCoinReward;
            RewardedAdCoinMultiplier = rewardedAdCoinMultiplier;
            CoinValuePerRewardImage = coinValuePerRewardImage;
            _boosterPrices = CopyBoosterPrices(boosterPrices);
        }

        public int LevelCompleteCoinReward { get; }

        public int RewardedAdCoinMultiplier { get; }

        public int CoinValuePerRewardImage { get; }

        public int GetBoosterPrice(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType);
            return _boosterPrices[boosterType];
        }

        private static ReadOnlyDictionary<BoosterType, int> CopyBoosterPrices(
            IReadOnlyDictionary<BoosterType, int> boosterPrices)
        {
            if (boosterPrices == null)
            {
                throw new ArgumentNullException(nameof(boosterPrices));
            }

            Dictionary<BoosterType, int> copiedPrices = new();

            foreach (KeyValuePair<BoosterType, int> boosterPrice in boosterPrices)
            {
                ValidateBoosterType(boosterPrice.Key);
                ValidatePositiveValue(
                    boosterPrice.Value,
                    nameof(boosterPrices));
                copiedPrices.Add(boosterPrice.Key, boosterPrice.Value);
            }

            foreach (BoosterType boosterType in AllBoosterTypes)
            {
                if (!copiedPrices.ContainsKey(boosterType))
                {
                    throw new ArgumentException(
                        $"Booster price for {boosterType} is missing.",
                        nameof(boosterPrices));
                }
            }

            return new ReadOnlyDictionary<BoosterType, int>(copiedPrices);
        }

        private static void ValidateBoosterType(BoosterType boosterType)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(boosterType),
                    boosterType,
                    "Booster type is not defined.");
            }
        }

        private static void ValidatePositiveValue(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Economy config value must be greater than zero.");
            }
        }
    }
}
