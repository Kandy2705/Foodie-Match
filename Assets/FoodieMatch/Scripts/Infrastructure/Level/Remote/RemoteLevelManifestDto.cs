using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelManifestDto
    {
        [JsonProperty("schemaVersion")]
        public int? SchemaVersion { get; set; }

        [JsonProperty("manifestVersion")]
        public int? ManifestVersion { get; set; }

        [JsonProperty("packs")]
        public List<RemoteLevelPackDto> Packs { get; set; }
    }

    public sealed class RemoteLevelPackDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("version")]
        public int? Version { get; set; }

        [JsonProperty("firstLevel")]
        public int? FirstLevel { get; set; }

        [JsonProperty("lastLevel")]
        public int? LastLevel { get; set; }

        [JsonProperty("archivePath")]
        public string ArchivePath { get; set; }

        [JsonProperty("archiveSha256")]
        public string ArchiveSha256 { get; set; }
    }
}
