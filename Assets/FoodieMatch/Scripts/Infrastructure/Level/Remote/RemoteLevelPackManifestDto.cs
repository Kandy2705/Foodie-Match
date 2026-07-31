using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelPackManifestDto
    {
        [JsonProperty("schemaVersion")]
        public int? SchemaVersion { get; set; }

        [JsonProperty("packId")]
        public int? PackId { get; set; }

        [JsonProperty("packVersion")]
        public int? PackVersion { get; set; }

        [JsonProperty("levels")]
        public List<RemoteLevelEntryDto> Levels { get; set; }
    }

    public sealed class RemoteLevelEntryDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("contentPath")]
        public string ContentPath { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }
    }
}
