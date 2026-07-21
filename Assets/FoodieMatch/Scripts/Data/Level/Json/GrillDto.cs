using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class GrillDto
    {
        [JsonProperty("position")]
        public GrillPositionDto Position { get; set; }

        [JsonProperty("foodIds")]
        public List<int> FoodIds { get; set; }

        [JsonProperty("trays")]
        public List<TrayDto> Trays { get; set; }
    }
}
