using Newtonsoft.Json;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class PackageSelectionSettingsDto
    {
        [JsonProperty("early")]
        public PackageSelectionWeightsDto Early { get; set; }

        [JsonProperty("middle")]
        public PackageSelectionWeightsDto Middle { get; set; }

        [JsonProperty("late")]
        public PackageSelectionWeightsDto Late { get; set; }
    }
}
