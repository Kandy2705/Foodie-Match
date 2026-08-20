using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.RemoteConfig;
using FoodieMatch.Core.Application.Configuration;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Infrastructure.GoldPass;
using FoodieMatch.Infrastructure.Persistence.Configuration;
using FoodieMatch.Infrastructure.Shop;
using UnityEngine;

namespace FoodieMatch.Infrastructure.RemoteConfig
{
    public sealed class FirebaseGameConfigurationLoader
    {
        private const ulong FetchTimeoutMilliseconds = 4_000;
        private const ulong MinimumFetchIntervalMilliseconds = 0;

        private readonly GameConfigurationSession _session;
        private readonly GameConfigurationSnapshotSet _localDefaults;
        private readonly PlayerPrefsGameConfigurationCache _cache;
        private readonly GameShopConfigSession _shopConfig;
        private readonly GameGoldPassConfigSession _goldPassConfig;
        private readonly PlayerPrefsGameCatalogCache _catalogCache;
        private readonly RemoteConfigSnapshotBuilder _snapshotBuilder = new();
        private readonly GameShopConfigJsonParser _shopConfigParser = new();
        private readonly GameGoldPassConfigJsonParser _goldPassConfigParser = new();

        public FirebaseGameConfigurationLoader(
            GameConfigurationSession session,
            GameConfigurationSnapshotSet localDefaults,
            PlayerPrefsGameConfigurationCache cache,
            GameShopConfigSession shopConfig,
            GameGoldPassConfigSession goldPassConfig,
            PlayerPrefsGameCatalogCache catalogCache)
        {
            _session = session;
            _localDefaults = localDefaults;
            _cache = cache;
            _shopConfig = shopConfig;
            _goldPassConfig = goldPassConfig;
            _catalogCache = catalogCache;
        }

        public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                FirebaseApp.LogLevel = LogLevel.Error;
                DependencyStatus dependencyStatus =
                    await FirebaseApp.CheckAndFixDependenciesAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogWarning(
                        $"Firebase dependencies are unavailable: {dependencyStatus}. " +
                        "Cached or local configuration will be used.");
                    return false;
                }

                FirebaseRemoteConfig remoteConfig =
                    FirebaseRemoteConfig.DefaultInstance;
                await remoteConfig.SetConfigSettingsAsync(
                    new ConfigSettings
                    {
                        FetchTimeoutInMilliseconds =
                            FetchTimeoutMilliseconds,
                        MinimumFetchIntervalInMilliseconds =
                            MinimumFetchIntervalMilliseconds
                    });
                await remoteConfig.SetDefaultsAsync(
                    CreateFirebaseDefaults(_localDefaults));
                await remoteConfig.FetchAsync(TimeSpan.Zero);
                cancellationToken.ThrowIfCancellationRequested();

                if (remoteConfig.Info.LastFetchStatus !=
                    LastFetchStatus.Success)
                {
                    Debug.LogWarning(
                        "Remote Config fetch failed. Cached or local configuration will be used.");
                    return false;
                }

                await remoteConfig.ActivateAsync();
                cancellationToken.ThrowIfCancellationRequested();

                GameConfigurationSnapshotSet configuration =
                    _snapshotBuilder.Build(remoteConfig, _session.Current);
                _session.Apply(configuration);
                _cache.Save(configuration);
                ApplyRemoteShopCatalog(remoteConfig);
                ApplyRemoteGoldPassCatalog(remoteConfig);
                return true;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Remote Config could not be refreshed: {exception.Message}. " +
                    "Cached or local configuration will be used.");
                return false;
            }
        }

        private void ApplyRemoteShopCatalog(
            FirebaseRemoteConfig remoteConfig)
        {
            ConfigValue value = remoteConfig.GetValue(
                FirebaseRemoteConfigKeys.ShopCatalog);

            if (value.Source != ValueSource.RemoteValue)
            {
                return;
            }

            string json = value.StringValue;

            if (!_shopConfigParser.TryParse(
                    json,
                    out IGameShopConfig config,
                    out string errorMessage))
            {
                Debug.LogWarning(
                    $"Remote Shop catalog was ignored: {errorMessage}");
                return;
            }

            if (!_shopConfig.TryApply(config))
            {
                Debug.LogWarning(
                    "Remote Shop catalog was ignored because its product IDs " +
                    "do not match the bundled Shop catalog.");
                return;
            }

            _catalogCache.SaveShopJson(json);
        }

        private void ApplyRemoteGoldPassCatalog(
            FirebaseRemoteConfig remoteConfig)
        {
            ConfigValue value = remoteConfig.GetValue(
                FirebaseRemoteConfigKeys.GoldPassCatalog);

            if (value.Source != ValueSource.RemoteValue)
            {
                return;
            }

            string json = value.StringValue;

            if (!_goldPassConfigParser.TryParse(
                    json,
                    out IGameGoldPassConfig config,
                    out string errorMessage))
            {
                Debug.LogWarning(
                    $"Remote Gold Pass catalog was ignored: {errorMessage}");
                return;
            }

            _goldPassConfig.Apply(config);
            _catalogCache.SaveGoldPassJson(json);
        }

        private static Dictionary<string, object> CreateFirebaseDefaults(
            GameConfigurationSnapshotSet defaults)
        {
            return new Dictionary<string, object>
            {
                [FirebaseRemoteConfigKeys.NormalLevelCompleteCoinReward] =
                    defaults.Economy.NormalLevelCompleteCoinReward,
                [FirebaseRemoteConfigKeys.HardLevelCompleteCoinReward] =
                    defaults.Economy.HardLevelCompleteCoinReward,
                [FirebaseRemoteConfigKeys.SuperHardLevelCompleteCoinReward] =
                    defaults.Economy.SuperHardLevelCompleteCoinReward,
                [FirebaseRemoteConfigKeys.RewardedAdCoinMultiplier] =
                    defaults.Economy.RewardedAdCoinMultiplier,
                [FirebaseRemoteConfigKeys.CoinValuePerRewardImage] =
                    defaults.Economy.CoinValuePerRewardImage,
                [FirebaseRemoteConfigKeys.FullHeartCoinPrice] =
                    defaults.Economy.FullHeartCoinPrice,
                [FirebaseRemoteConfigKeys.PlateBoosterPrice] =
                    defaults.Economy.GetBoosterPrice(BoosterType.Plate),
                [FirebaseRemoteConfigKeys.StorageBoosterPrice] =
                    defaults.Economy.GetBoosterPrice(BoosterType.Storage),
                [FirebaseRemoteConfigKeys.SwapBoosterPrice] =
                    defaults.Economy.GetBoosterPrice(BoosterType.Swap),
                [FirebaseRemoteConfigKeys.FridgeBoosterPrice] =
                    defaults.Economy.GetBoosterPrice(BoosterType.Fridge),
                [FirebaseRemoteConfigKeys.BoxBoosterPrice] =
                    defaults.Economy.GetBoosterPrice(BoosterType.Box),
                [FirebaseRemoteConfigKeys.MaxHeartCount] =
                    defaults.Heart.MaxHeartCount,
                [FirebaseRemoteConfigKeys.HeartRecoveryMinutes] =
                    checked((int)defaults.Heart.HeartRecoveryDuration.TotalMinutes),
                [FirebaseRemoteConfigKeys.PlateBoosterUnlockLevel] =
                    defaults.Booster.GetUnlockLevel(BoosterType.Plate),
                [FirebaseRemoteConfigKeys.StorageBoosterUnlockLevel] =
                    defaults.Booster.GetUnlockLevel(BoosterType.Storage),
                [FirebaseRemoteConfigKeys.SwapBoosterUnlockLevel] =
                    defaults.Booster.GetUnlockLevel(BoosterType.Swap),
                [FirebaseRemoteConfigKeys.FridgeBoosterUnlockLevel] =
                    defaults.Booster.GetUnlockLevel(BoosterType.Fridge),
                [FirebaseRemoteConfigKeys.BoxBoosterUnlockLevel] =
                    defaults.Booster.GetUnlockLevel(BoosterType.Box),
                [FirebaseRemoteConfigKeys.BoosterUnlockRewardAmount] =
                    defaults.Booster.UnlockRewardAmount,
                [FirebaseRemoteConfigKeys.GoldPassUnlockLevel] =
                    defaults.GoldPassProgression.UnlockLevel,
                [FirebaseRemoteConfigKeys.GoldPassNormalSpoonsPerCompletedLevel] =
                    defaults.GoldPassProgression.NormalSpoonsPerCompletedLevel,
                [FirebaseRemoteConfigKeys.GoldPassHardSpoonsPerCompletedLevel] =
                    defaults.GoldPassProgression.HardSpoonsPerCompletedLevel,
                [FirebaseRemoteConfigKeys.GoldPassSuperHardSpoonsPerCompletedLevel] =
                    defaults.GoldPassProgression.SuperHardSpoonsPerCompletedLevel,
                [FirebaseRemoteConfigKeys.PostLevelAdStartLevel] =
                    defaults.Ads.PostLevelAdStartLevel,
                [FirebaseRemoteConfigKeys.PostLevelAdIntervalMinutes] =
                    checked((int)defaults.Ads.PostLevelAdInterval.TotalMinutes),
                [FirebaseRemoteConfigKeys.LevelManifestVersion] = 0,
                [FirebaseRemoteConfigKeys.LevelManifestUrl] = string.Empty
            };
        }
    }
}
