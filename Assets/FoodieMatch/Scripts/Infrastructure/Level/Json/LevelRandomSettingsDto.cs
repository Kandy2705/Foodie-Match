using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelRandomSettingsDto
    {
        [JsonProperty("packageSeeds")]
        public List<int> PackageSeeds { get; set; }

        [JsonProperty("generatePackageSeedEachRun")]
        public bool? GeneratePackageSeedEachRun { get; set; }

        [JsonProperty("randomizeFoodVisualsEachRun")]
        public bool? RandomizeFoodVisualsEachRun { get; set; }

        [JsonProperty("fixedFoodVisualSeed")]
        public int? FixedFoodVisualSeed { get; set; }
    }
}
