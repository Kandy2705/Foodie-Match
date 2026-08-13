using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Configuration;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Infrastructure.Persistence.Save;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Persistence.Configuration
{
    public sealed class PlayerPrefsGameConfigurationCache
    {
        private const string CacheKey = "GameConfiguration";
        private const int CurrentSchemaVersion = 3;

        private readonly ISaveService _saveService;

        public PlayerPrefsGameConfigurationCache(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public bool TryLoad(out GameConfigurationSnapshotSet configuration)
        {
            if (!_saveService.HasKey(CacheKey))
            {
                configuration = null;
                return false;
            }

            try
            {
                GameConfigurationCacheDto dto =
                    JsonConvert.DeserializeObject<GameConfigurationCacheDto>(
                        _saveService.GetString(CacheKey, string.Empty));

                if (dto == null)
                {
                    throw new JsonSerializationException(
                        "Game configuration cache is empty.");
                }

                if (dto.SchemaVersion != CurrentSchemaVersion)
                {
                    throw new JsonSerializationException(
                        $"Unsupported game configuration schema version {dto.SchemaVersion}.");
                }

                configuration = CreateSnapshotSet(dto);
                return true;
            }
            catch (Exception exception) when (
                exception is JsonException ||
                exception is ArgumentException ||
                exception is OverflowException)
            {
                _saveService.DeleteKey(CacheKey);
                _saveService.Save();
                configuration = null;
                return false;
            }
        }

        public void Save(GameConfigurationSnapshotSet configuration)
        {
            GameConfigurationCacheDto dto = CreateDto(configuration);
            _saveService.SetString(
                CacheKey,
                JsonConvert.SerializeObject(dto, Formatting.None));
            _saveService.Save();
        }

        private static GameConfigurationSnapshotSet CreateSnapshotSet(
            GameConfigurationCacheDto dto)
        {
            Dictionary<BoosterType, int> boosterPrices = new()
            {
                [BoosterType.Plate] = dto.Economy.PlateBoosterPrice,
                [BoosterType.Storage] = dto.Economy.StorageBoosterPrice,
                [BoosterType.Swap] = dto.Economy.SwapBoosterPrice,
                [BoosterType.Fridge] = dto.Economy.FridgeBoosterPrice,
                [BoosterType.Box] = dto.Economy.BoxBoosterPrice
            };
            Dictionary<BoosterType, int> unlockLevels = new()
            {
                [BoosterType.Plate] = dto.Booster.PlateUnlockLevel,
                [BoosterType.Storage] = dto.Booster.StorageUnlockLevel,
                [BoosterType.Swap] = dto.Booster.SwapUnlockLevel,
                [BoosterType.Fridge] = dto.Booster.FridgeUnlockLevel,
                [BoosterType.Box] = dto.Booster.BoxUnlockLevel
            };

            return new GameConfigurationSnapshotSet(
                new GameEconomyConfigSnapshot(
                    dto.Economy.LevelCompleteCoinReward,
                    dto.Economy.RewardedAdCoinMultiplier,
                    dto.Economy.CoinValuePerRewardImage,
                    dto.Economy.FullHeartCoinPrice,
                    boosterPrices),
                new GameHeartConfigSnapshot(
                    dto.Heart.MaxCount,
                    TimeSpan.FromMinutes(dto.Heart.RecoveryMinutes)),
                new GameBoosterConfigSnapshot(
                    unlockLevels,
                    dto.Booster.UnlockRewardAmount),
                new GameAdsConfigSnapshot(
                    TimeSpan.FromMinutes(dto.Ads.PostLevelIntervalMinutes)),
                new GameGoldPassProgressionConfigSnapshot(
                    dto.GoldPassProgression.SpoonsPerCompletedLevel));
        }

        private static GameConfigurationCacheDto CreateDto(
            GameConfigurationSnapshotSet configuration)
        {
            return new GameConfigurationCacheDto
            {
                SchemaVersion = CurrentSchemaVersion,
                Economy = new EconomyConfigDto
                {
                    LevelCompleteCoinReward =
                        configuration.Economy.LevelCompleteCoinReward,
                    RewardedAdCoinMultiplier =
                        configuration.Economy.RewardedAdCoinMultiplier,
                    CoinValuePerRewardImage =
                        configuration.Economy.CoinValuePerRewardImage,
                    FullHeartCoinPrice =
                        configuration.Economy.FullHeartCoinPrice,
                    PlateBoosterPrice =
                        configuration.Economy.GetBoosterPrice(BoosterType.Plate),
                    StorageBoosterPrice =
                        configuration.Economy.GetBoosterPrice(BoosterType.Storage),
                    SwapBoosterPrice =
                        configuration.Economy.GetBoosterPrice(BoosterType.Swap),
                    FridgeBoosterPrice =
                        configuration.Economy.GetBoosterPrice(BoosterType.Fridge),
                    BoxBoosterPrice =
                        configuration.Economy.GetBoosterPrice(BoosterType.Box)
                },
                Heart = new HeartConfigDto
                {
                    MaxCount = configuration.Heart.MaxHeartCount,
                    RecoveryMinutes =
                        checked((int)configuration.Heart.HeartRecoveryDuration.TotalMinutes)
                },
                Booster = new BoosterConfigDto
                {
                    PlateUnlockLevel =
                        configuration.Booster.GetUnlockLevel(BoosterType.Plate),
                    StorageUnlockLevel =
                        configuration.Booster.GetUnlockLevel(BoosterType.Storage),
                    SwapUnlockLevel =
                        configuration.Booster.GetUnlockLevel(BoosterType.Swap),
                    FridgeUnlockLevel =
                        configuration.Booster.GetUnlockLevel(BoosterType.Fridge),
                    BoxUnlockLevel =
                        configuration.Booster.GetUnlockLevel(BoosterType.Box),
                    UnlockRewardAmount =
                        configuration.Booster.UnlockRewardAmount
                },
                Ads = new AdsConfigDto
                {
                    PostLevelIntervalMinutes =
                        checked((int)configuration.Ads.PostLevelAdInterval.TotalMinutes)
                },
                GoldPassProgression = new GoldPassProgressionConfigDto
                {
                    SpoonsPerCompletedLevel = configuration
                        .GoldPassProgression
                        .SpoonsPerCompletedLevel
                }
            };
        }
    }
}
