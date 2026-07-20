using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class LevelCatalogDto
    {
        [JsonProperty("schemaVersion")]
        public int? SchemaVersion { get; set; }

        [JsonProperty("levelOrder")]
        public List<int> LevelOrder { get; set; }

        [JsonProperty("levels")]
        public List<LevelDto> Levels { get; set; }
    }
}
