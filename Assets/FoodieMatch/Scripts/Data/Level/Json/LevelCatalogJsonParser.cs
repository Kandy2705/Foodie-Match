using System;
using System.Globalization;
using Newtonsoft.Json;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class LevelCatalogJsonParser
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Culture = CultureInfo.InvariantCulture,
            MissingMemberHandling = MissingMemberHandling.Error
        };

        public bool TryParse(
            string json,
            out LevelCatalogDto catalog,
            out string error)
        {
            catalog = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Level catalog JSON cannot be empty.";
                return false;
            }

            try
            {
                catalog = JsonConvert.DeserializeObject<LevelCatalogDto>(
                    json,
                    _serializerSettings);
            }
            catch (JsonException exception)
            {
                error = $"Level catalog JSON could not be parsed: {exception.Message}";
                return false;
            }

            if (catalog != null)
            {
                return true;
            }

            error = "Level catalog JSON produced no data.";
            return false;
        }
    }
}
