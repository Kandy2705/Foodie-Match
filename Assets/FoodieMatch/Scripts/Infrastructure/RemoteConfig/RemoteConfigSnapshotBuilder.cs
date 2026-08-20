using System;
using System.Collections.Generic;
using Firebase.RemoteConfig;
using FoodieMatch.Core.Application.Configuration;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Domain.Booster;
using UnityEngine;

namespace FoodieMatch.Infrastructure.RemoteConfig
{
    internal sealed class RemoteConfigSnapshotBuilder
    {
        public GameConfigurationSnapshotSet Build(
            FirebaseRemoteConfig remoteConfig,
            GameConfigurationSnapshotSet fallback)
        {
            Dictionary<BoosterType, int> boosterPrices = new()
            {
                [BoosterType.Plate] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.PlateBoosterPrice,
                    fallback.Economy.GetBoosterPrice(BoosterType.Plate)),
                [BoosterType.Storage] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.StorageBoosterPrice,
                    fallback.Economy.GetBoosterPrice(BoosterType.Storage)),
                [BoosterType.Swap] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.SwapBoosterPrice,
                    fallback.Economy.GetBoosterPrice(BoosterType.Swap)),
                [BoosterType.Fridge] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.FridgeBoosterPrice,
                    fallback.Economy.GetBoosterPrice(BoosterType.Fridge)),
                [BoosterType.Box] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.BoxBoosterPrice,
                    fallback.Economy.GetBoosterPrice(BoosterType.Box))
            };
            Dictionary<BoosterType, int> unlockLevels = new()
            {
                [BoosterType.Plate] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.PlateBoosterUnlockLevel,
                    fallback.Booster.GetUnlockLevel(BoosterType.Plate)),
                [BoosterType.Storage] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.StorageBoosterUnlockLevel,
                    fallback.Booster.GetUnlockLevel(BoosterType.Storage)),
                [BoosterType.Swap] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.SwapBoosterUnlockLevel,
                    fallback.Booster.GetUnlockLevel(BoosterType.Swap)),
                [BoosterType.Fridge] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.FridgeBoosterUnlockLevel,
                    fallback.Booster.GetUnlockLevel(BoosterType.Fridge)),
                [BoosterType.Box] = ReadPositiveInt(
                    remoteConfig,
                    FirebaseRemoteConfigKeys.BoxBoosterUnlockLevel,
                    fallback.Booster.GetUnlockLevel(BoosterType.Box))
            };

            return new GameConfigurationSnapshotSet(
                new GameEconomyConfigSnapshot(
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.NormalLevelCompleteCoinReward,
                        fallback.Economy.NormalLevelCompleteCoinReward),
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.HardLevelCompleteCoinReward,
                        fallback.Economy.HardLevelCompleteCoinReward),
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.SuperHardLevelCompleteCoinReward,
                        fallback.Economy.SuperHardLevelCompleteCoinReward),
                    ReadIntGreaterThanOne(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.RewardedAdCoinMultiplier,
                        fallback.Economy.RewardedAdCoinMultiplier),
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.CoinValuePerRewardImage,
                        fallback.Economy.CoinValuePerRewardImage),
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.FullHeartCoinPrice,
                        fallback.Economy.FullHeartCoinPrice),
                    boosterPrices),
                new GameHeartConfigSnapshot(
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.MaxHeartCount,
                        fallback.Heart.MaxHeartCount),
                    TimeSpan.FromMinutes(
                        ReadPositiveInt(
                            remoteConfig,
                            FirebaseRemoteConfigKeys.HeartRecoveryMinutes,
                            checked((int)fallback.Heart.HeartRecoveryDuration.TotalMinutes)))),
                new GameBoosterConfigSnapshot(
                    unlockLevels,
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.BoosterUnlockRewardAmount,
                        fallback.Booster.UnlockRewardAmount)),
                new GameAdsConfigSnapshot(
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.PostLevelAdStartLevel,
                        fallback.Ads.PostLevelAdStartLevel),
                    TimeSpan.FromMinutes(
                        ReadPositiveInt(
                            remoteConfig,
                            FirebaseRemoteConfigKeys.PostLevelAdIntervalMinutes,
                            checked((int)fallback.Ads.PostLevelAdInterval.TotalMinutes)))),
                new GameGoldPassProgressionConfigSnapshot(
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.GoldPassUnlockLevel,
                        fallback.GoldPassProgression.UnlockLevel),
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.GoldPassNormalSpoonsPerCompletedLevel,
                        fallback.GoldPassProgression.NormalSpoonsPerCompletedLevel),
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.GoldPassHardSpoonsPerCompletedLevel,
                        fallback.GoldPassProgression.HardSpoonsPerCompletedLevel),
                    ReadPositiveInt(
                        remoteConfig,
                        FirebaseRemoteConfigKeys.GoldPassSuperHardSpoonsPerCompletedLevel,
                        fallback.GoldPassProgression.SuperHardSpoonsPerCompletedLevel)));
        }

        private static int ReadPositiveInt(
            FirebaseRemoteConfig remoteConfig,
            string key,
            int fallback)
        {
            return ReadInt(remoteConfig, key, fallback, value => value > 0);
        }

        private static int ReadIntGreaterThanOne(
            FirebaseRemoteConfig remoteConfig,
            string key,
            int fallback)
        {
            return ReadInt(remoteConfig, key, fallback, value => value > 1);
        }

        private static int ReadInt(
            FirebaseRemoteConfig remoteConfig,
            string key,
            int fallback,
            Func<int, bool> isValid)
        {
            ConfigValue configValue = remoteConfig.GetValue(key);

            if (configValue.Source != ValueSource.RemoteValue)
            {
                return fallback;
            }

            try
            {
                long remoteValue = configValue.LongValue;

                if (remoteValue < int.MinValue || remoteValue > int.MaxValue)
                {
                    LogInvalidValue(key);
                    return fallback;
                }

                int value = (int)remoteValue;

                if (!isValid(value))
                {
                    LogInvalidValue(key);
                    return fallback;
                }

                return value;
            }
            catch (FormatException)
            {
                LogInvalidValue(key);
                return fallback;
            }
        }

        private static void LogInvalidValue(string key)
        {
            Debug.LogWarning(
                $"Remote Config value for '{key}' is invalid. Cached or local value will be used.");
        }
    }
}
