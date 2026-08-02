using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelContentDto
    {
        [JsonProperty("schemaVersion")]
        public int? SchemaVersion { get; set; }

        [JsonProperty("level")]
        public LevelDto Level { get; set; }
    }
}
