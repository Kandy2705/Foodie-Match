using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Persistence.Configuration
{
    internal sealed class GameConfigurationCacheDto
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }

        [JsonProperty("economy", Required = Required.Always)]
        public EconomyConfigDto Economy { get; set; }

        [JsonProperty("heart", Required = Required.Always)]
        public HeartConfigDto Heart { get; set; }

        [JsonProperty("booster", Required = Required.Always)]
        public BoosterConfigDto Booster { get; set; }

        [JsonProperty("ads", Required = Required.Always)]
        public AdsConfigDto Ads { get; set; }
    }

    internal sealed class EconomyConfigDto
    {
        [JsonProperty("levelCompleteCoinReward", Required = Required.Always)]
        public int LevelCompleteCoinReward { get; set; }

        [JsonProperty("rewardedAdCoinMultiplier", Required = Required.Always)]
        public int RewardedAdCoinMultiplier { get; set; }

        [JsonProperty("coinValuePerRewardImage", Required = Required.Always)]
        public int CoinValuePerRewardImage { get; set; }

        [JsonProperty("fullHeartCoinPrice", Required = Required.Always)]
        public int FullHeartCoinPrice { get; set; }

        [JsonProperty("plateBoosterPrice", Required = Required.Always)]
        public int PlateBoosterPrice { get; set; }

        [JsonProperty("storageBoosterPrice", Required = Required.Always)]
        public int StorageBoosterPrice { get; set; }

        [JsonProperty("swapBoosterPrice", Required = Required.Always)]
        public int SwapBoosterPrice { get; set; }

        [JsonProperty("fridgeBoosterPrice", Required = Required.Always)]
        public int FridgeBoosterPrice { get; set; }

        [JsonProperty("boxBoosterPrice", Required = Required.Always)]
        public int BoxBoosterPrice { get; set; }
    }

    internal sealed class HeartConfigDto
    {
        [JsonProperty("maxCount", Required = Required.Always)]
        public int MaxCount { get; set; }

        [JsonProperty("recoveryMinutes", Required = Required.Always)]
        public int RecoveryMinutes { get; set; }
    }

    internal sealed class BoosterConfigDto
    {
        [JsonProperty("plateUnlockLevel", Required = Required.Always)]
        public int PlateUnlockLevel { get; set; }

        [JsonProperty("storageUnlockLevel", Required = Required.Always)]
        public int StorageUnlockLevel { get; set; }

        [JsonProperty("swapUnlockLevel", Required = Required.Always)]
        public int SwapUnlockLevel { get; set; }

        [JsonProperty("fridgeUnlockLevel", Required = Required.Always)]
        public int FridgeUnlockLevel { get; set; }

        [JsonProperty("boxUnlockLevel", Required = Required.Always)]
        public int BoxUnlockLevel { get; set; }

        [JsonProperty("unlockRewardAmount", Required = Required.Always)]
        public int UnlockRewardAmount { get; set; }
    }

    internal sealed class AdsConfigDto
    {
        [JsonProperty("postLevelIntervalMinutes", Required = Required.Always)]
        public int PostLevelIntervalMinutes { get; set; }
    }
}
