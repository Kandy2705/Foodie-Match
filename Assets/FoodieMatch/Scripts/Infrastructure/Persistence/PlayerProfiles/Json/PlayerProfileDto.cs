using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class PlayerProfileDto
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }

        [JsonProperty("revision", Required = Required.Always)]
        public long Revision { get; set; }

        [JsonProperty("currentLevelNumber", Required = Required.Always)]
        public int CurrentLevelNumber { get; set; }

        [JsonProperty("coinBalance", Required = Required.Always)]
        public long CoinBalance { get; set; }

        [JsonProperty("boosterCounts", Required = Required.Always)]
        public List<BoosterCountDto> BoosterCounts { get; set; }

        [JsonProperty("seenBoosterGuides", Required = Required.Always)]
        public List<int> SeenBoosterGuides { get; set; }
    }
}
