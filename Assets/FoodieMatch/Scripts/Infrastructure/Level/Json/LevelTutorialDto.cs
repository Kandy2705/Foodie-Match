using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelTutorialDto
    {
        [JsonProperty("foodSelectionSequence")]
        public List<FoodSelectionTutorialStepDto> FoodSelectionSequence { get; set; }
    }
}
