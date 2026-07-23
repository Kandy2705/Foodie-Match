using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class GrillPositionDto
    {
        [JsonProperty("x")]
        public float? X { get; set; }

        [JsonProperty("y")]
        public float? Y { get; set; }
    }
}
