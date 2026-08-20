using System;
using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Configuration.Heart;
using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Configuration
{
    public sealed class GameConfigurationSession :
        IGameEconomyConfig,
        IGameHeartConfig,
        IGameBoosterConfig,
        IGameAdsConfig,
        IGameGoldPassProgressionConfig
    {
        private GameConfigurationSnapshotSet _current;

        public GameConfigurationSession(GameConfigurationSnapshotSet initial)
        {
            _current = initial;
        }

        public GameConfigurationSnapshotSet Current => _current;

        public int RewardedAdCoinMultiplier => _current.Economy.RewardedAdCoinMultiplier;

        public int CoinValuePerRewardImage => _current.Economy.CoinValuePerRewardImage;

        public int FullHeartCoinPrice => _current.Economy.FullHeartCoinPrice;

        public int MaxHeartCount => _current.Heart.MaxHeartCount;

        public TimeSpan HeartRecoveryDuration => _current.Heart.HeartRecoveryDuration;

        public int PostLevelAdStartLevel => _current.Ads.PostLevelAdStartLevel;

        public TimeSpan PostLevelAdInterval => _current.Ads.PostLevelAdInterval;

        public int UnlockRewardAmount => _current.Booster.UnlockRewardAmount;

        public int UnlockLevel => _current.GoldPassProgression.UnlockLevel;

        public int GetLevelCompleteCoinReward(LevelDifficulty difficulty)
        {
            return _current.Economy.GetLevelCompleteCoinReward(difficulty);
        }

        public int GetSpoonsPerCompletedLevel(LevelDifficulty difficulty)
        {
            return _current.GoldPassProgression
                .GetSpoonsPerCompletedLevel(difficulty);
        }

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
