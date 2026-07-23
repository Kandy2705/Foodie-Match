using System.Collections.Generic;
using FoodieMatch.Data.Booster;

namespace FoodieMatch.Core.Application.Configuration.Economy
{
    public static class GameEconomyDefaults
    {
        private const int DefaultLevelCompleteCoinReward = 40;
        private const int DefaultRewardedAdCoinMultiplier = 2;
        private const int DefaultCoinValuePerRewardImage = 5;
        private const int DefaultPlateBoosterPrice = 80;
        private const int DefaultStorageBoosterPrice = 60;
        private const int DefaultSwapBoosterPrice = 120;
        private const int DefaultFridgeBoosterPrice = 120;
        private const int DefaultBoxBoosterPrice = 190;

        public static GameEconomyConfigSnapshot CreateSnapshot()
        {
            Dictionary<BoosterType, int> boosterPrices = new()
            {
                [BoosterType.Plate] = DefaultPlateBoosterPrice,
                [BoosterType.Storage] = DefaultStorageBoosterPrice,
                [BoosterType.Swap] = DefaultSwapBoosterPrice,
                [BoosterType.Fridge] = DefaultFridgeBoosterPrice,
                [BoosterType.Box] = DefaultBoxBoosterPrice
            };

            return new GameEconomyConfigSnapshot(
                DefaultLevelCompleteCoinReward,
                DefaultRewardedAdCoinMultiplier,
                DefaultCoinValuePerRewardImage,
                boosterPrices);
        }
    }
}
