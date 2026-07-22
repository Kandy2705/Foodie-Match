using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json
{
    internal sealed class PlayerProfileJsonParser
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        public bool TryReadSchemaVersion(
            string json,
            out int schemaVersion,
            out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                schemaVersion = 0;
                errorMessage = "Player profile JSON is empty.";
                return false;
            }

            try
            {
                JObject profileObject = JObject.Parse(json);

                if (!profileObject.TryGetValue(
                        "schemaVersion",
                        StringComparison.Ordinal,
                        out JToken schemaVersionToken) ||
                    schemaVersionToken.Type != JTokenType.Integer)
                {
                    schemaVersion = 0;
                    errorMessage = "Player profile schema version is missing or invalid.";
                    return false;
                }

                if (!long.TryParse(
                        schemaVersionToken.ToString(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out long storedVersion) ||
                    storedVersion < int.MinValue ||
                    storedVersion > int.MaxValue)
                {
                    schemaVersion = 0;
                    errorMessage = "Player profile schema version is outside the supported range.";
                    return false;
                }

                schemaVersion = (int)storedVersion;
                errorMessage = null;
                return true;
            }
            catch (JsonException exception)
            {
                schemaVersion = 0;
                errorMessage = exception.Message;
                return false;
            }
        }

        public bool TryDeserialize(
            string json,
            out PlayerProfileDto profileDto,
            out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                profileDto = null;
                errorMessage = "Player profile JSON is empty.";
                return false;
            }

            try
            {
                profileDto = JsonConvert.DeserializeObject<PlayerProfileDto>(
                    json,
                    _serializerSettings);

                if (profileDto == null)
                {
                    errorMessage = "Player profile JSON does not contain an object.";
                    return false;
                }

                errorMessage = null;
                return true;
            }
            catch (JsonException exception)
            {
                profileDto = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        public bool TrySerialize(
            PlayerProfileDto profileDto,
            out string json,
            out string errorMessage)
        {
            if (profileDto == null)
            {
                throw new ArgumentNullException(nameof(profileDto));
            }

            try
            {
                json = JsonConvert.SerializeObject(
                    profileDto,
                    Formatting.None,
                    _serializerSettings);
                errorMessage = null;
                return true;
            }
            catch (JsonException exception)
            {
                json = null;
                errorMessage = exception.Message;
                return false;
            }
        }
    }
}
