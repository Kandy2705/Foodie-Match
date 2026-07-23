using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class PackageSelectionWeightsDto
    {
        [JsonProperty("rackRescue")]
        public int? RackRescue { get; set; }

        [JsonProperty("readyNow")]
        public int? ReadyNow { get; set; }

        [JsonProperty("topTray")]
        public int? TopTray { get; set; }
    }
}
