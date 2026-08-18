using FoodieMatch.Core.Application.Configuration.Advertising;
using FoodieMatch.Core.Application.Configuration.Booster;
using FoodieMatch.Core.Application.Configuration.Economy;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Configuration.Heart;

namespace FoodieMatch.Core.Application.Configuration
{
    public sealed class GameConfigurationSnapshotSet
    {
        public GameConfigurationSnapshotSet(
            GameEconomyConfigSnapshot economy,
            GameHeartConfigSnapshot heart,
            GameBoosterConfigSnapshot booster,
            GameAdsConfigSnapshot ads,
            GameGoldPassProgressionConfigSnapshot goldPassProgression)
        {
            Economy = economy;
            Heart = heart;
            Booster = booster;
            Ads = ads;
            GoldPassProgression = goldPassProgression;
        }

        public GameEconomyConfigSnapshot Economy { get; }

        public GameHeartConfigSnapshot Heart { get; }

        public GameBoosterConfigSnapshot Booster { get; }

        public GameAdsConfigSnapshot Ads { get; }

        public GameGoldPassProgressionConfigSnapshot GoldPassProgression { get; }

        public static GameConfigurationSnapshotSet CreateDefaults()
        {
            return new GameConfigurationSnapshotSet(
                GameEconomyDefaults.CreateSnapshot(),
                GameHeartDefaults.CreateSnapshot(),
                GameBoosterDefaults.CreateSnapshot(),
                GameAdsDefaults.CreateSnapshot(),
                GameGoldPassProgressionDefaults.CreateSnapshot());
        }
    }
}
