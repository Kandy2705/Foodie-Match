using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Configuration.Economy
{
    public interface IGameEconomyConfig
    {
        int RewardedAdCoinMultiplier { get; }

        int CoinValuePerRewardImage { get; }

        int FullHeartCoinPrice { get; }

        int GetLevelCompleteCoinReward(LevelDifficulty difficulty);

        int GetBoosterPrice(BoosterType boosterType);
    }
}
