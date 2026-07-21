using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class BoosterCountDto
    {
        [JsonProperty("boosterType", Required = Required.Always)]
        public int BoosterType { get; set; }

        [JsonProperty("count", Required = Required.Always)]
        public int Count { get; set; }
    }
}
