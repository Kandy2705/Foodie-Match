using FoodieMatch.Data.Booster;

namespace FoodieMatch.Core.Application.Configuration.Economy
{
    public interface IGameEconomyConfig
    {
        int LevelCompleteCoinReward { get; }

        int RewardedAdCoinMultiplier { get; }

        int CoinValuePerParticle { get; }

        int GetBoosterPrice(BoosterType boosterType);
    }
}
