using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class FoodSelectionTutorialStepDto
    {
        [JsonProperty("grillId")]
        public int? GrillId { get; set; }

        [JsonProperty("foodSlotIndex")]
        public int? FoodSlotIndex { get; set; }
    }
}
