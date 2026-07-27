using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.Economy
{
    public interface IGameEconomyConfig
    {
        int LevelCompleteCoinReward { get; }

        int RewardedAdCoinMultiplier { get; }

        int CoinValuePerRewardImage { get; }

        int FullHeartCoinPrice { get; }

        int GetBoosterPrice(BoosterType boosterType);
    }
}
