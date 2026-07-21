using System.Collections.Generic;
using Newtonsoft.Json;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class TrayDto
    {
        [JsonProperty("foodIds")]
        public List<int> FoodIds { get; set; }
    }
}
