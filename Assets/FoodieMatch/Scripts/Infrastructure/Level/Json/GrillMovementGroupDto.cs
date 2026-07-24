using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class GrillMovementGroupDto
    {
        [JsonProperty("direction")]
        public string Direction { get; set; }

        [JsonProperty("grillIds")]
        public List<int> GrillIds { get; set; }

        [JsonProperty("movementSpeed")]
        public float? MovementSpeed { get; set; }
    }
}
