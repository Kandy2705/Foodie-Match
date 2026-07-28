using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class StackedGrillColumnDto
    {
        [JsonProperty("grillIds")]
        public List<int> GrillIds { get; set; }
    }
}
