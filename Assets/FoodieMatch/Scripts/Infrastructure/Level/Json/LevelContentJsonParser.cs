using System;
using System.Globalization;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelContentJsonParser
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Culture = CultureInfo.InvariantCulture,
            MissingMemberHandling = MissingMemberHandling.Error
        };

        public bool TryParse(
            string json,
            out LevelContentDto content,
            out string error)
        {
            content = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Level content JSON cannot be empty.";
                return false;
            }

            try
            {
                content = JsonConvert.DeserializeObject<LevelContentDto>(
                    json,
                    _serializerSettings);
            }
            catch (JsonException exception)
            {
                error = $"Level content JSON could not be parsed: {exception.Message}";
                return false;
            }

            if (content != null)
            {
                return true;
            }

            error = "Level content JSON produced no data.";
            return false;
        }
    }
}
