using System;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration
{
    public sealed class GameConfigurationSession :
        IGameEconomyConfig,
        IGameHeartConfig,
        IGameBoosterConfig,
        IGameAdsConfig
    {
        private GameConfigurationSnapshotSet _current;

        public GameConfigurationSession(GameConfigurationSnapshotSet initial)
        {
            _current = initial;
        }

        public GameConfigurationSnapshotSet Current => _current;

        public int LevelCompleteCoinReward => _current.Economy.LevelCompleteCoinReward;

        public int RewardedAdCoinMultiplier => _current.Economy.RewardedAdCoinMultiplier;

        public int CoinValuePerRewardImage => _current.Economy.CoinValuePerRewardImage;

        public int FullHeartCoinPrice => _current.Economy.FullHeartCoinPrice;

        public int MaxHeartCount => _current.Heart.MaxHeartCount;

        public TimeSpan HeartRecoveryDuration => _current.Heart.HeartRecoveryDuration;

        public TimeSpan PostLevelAdInterval => _current.Ads.PostLevelAdInterval;

        public int UnlockRewardAmount => _current.Booster.UnlockRewardAmount;

        public int GetBoosterPrice(BoosterType boosterType)
        {
            return _current.Economy.GetBoosterPrice(boosterType);
        }

        public int GetUnlockLevel(BoosterType boosterType)
        {
            return _current.Booster.GetUnlockLevel(boosterType);
        }

        public void Apply(GameConfigurationSnapshotSet configuration)
        {
            _current = configuration;
        }
    }
}
