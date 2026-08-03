using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("grillLayoutType")]
        public string GrillLayoutType { get; set; }

        [JsonProperty("randomization")]
        public LevelRandomSettingsDto RandomSettings { get; set; }

        [JsonProperty("packageSelectionWeights")]
        public PackageSelectionSettingsDto PackageSelectionSettings { get; set; }

        [JsonProperty("tutorial")]
        public LevelTutorialDto Tutorial { get; set; }

        [JsonProperty("movingGrillGroups")]
        public List<GrillMovementGroupDto> MovingGrillGroups { get; set; }

        [JsonProperty("grillColumns")]
        public List<StackedGrillColumnDto> StackedGrillColumns { get; set; }

        [JsonProperty("grills")]
        public List<GrillDto> Grills { get; set; }
    }
}
