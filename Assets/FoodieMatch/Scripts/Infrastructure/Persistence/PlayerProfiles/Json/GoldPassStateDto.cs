using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class GoldPassStateDto
    {
        [JsonProperty("seasonId", Required = Required.Always)]
        public string SeasonId { get; set; }

        [JsonProperty("spoonCount", Required = Required.Always)]
        public int SpoonCount { get; set; }

        [JsonProperty("isSeasonPassPurchased", Required = Required.Always)]
        public bool IsSeasonPassPurchased { get; set; }

        [JsonProperty("claimedFreeMilestoneLevels", Required = Required.Always)]
        public List<int> ClaimedFreeMilestoneLevels { get; set; }

        [JsonProperty("claimedSeasonMilestoneLevels", Required = Required.Always)]
        public List<int> ClaimedSeasonMilestoneLevels { get; set; }
    }
}
