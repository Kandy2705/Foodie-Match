using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelCatalogEntryDto
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("contentFile")]
        public string ContentFile { get; set; }
    }
}
