using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class GrillDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("position")]
        public GrillPositionDto Position { get; set; }

        [JsonProperty("foodIds")]
        public List<int> FoodIds { get; set; }

        [JsonProperty("trays")]
        public List<TrayDto> Trays { get; set; }
    }
}
