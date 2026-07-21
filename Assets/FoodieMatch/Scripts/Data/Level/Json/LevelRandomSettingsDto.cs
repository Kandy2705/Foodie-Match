using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class LevelRandomSettingsDto
    {
        [JsonProperty("packageSeeds")]
        public List<int> PackageSeeds { get; set; }

        [JsonProperty("randomizePackageSelectionEachRun")]
        public bool? RandomizePackageSelectionEachRun { get; set; }

        [JsonProperty("randomizeFoodVisualsEachRun")]
        public bool? RandomizeFoodVisualsEachRun { get; set; }

        [JsonProperty("fixedFoodVisualSeed")]
        public int? FixedFoodVisualSeed { get; set; }
    }
}
