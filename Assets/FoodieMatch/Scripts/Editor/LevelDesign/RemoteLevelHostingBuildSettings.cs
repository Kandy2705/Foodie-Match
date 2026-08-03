using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class RemoteLevelHostingBuildSettings
    {
        [JsonProperty("manifestVersion")]
        public int ManifestVersion { get; set; }

        [JsonProperty("packVersions")]
        public List<int> PackVersions { get; set; }
    }
}
