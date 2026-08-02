using System.Globalization;
using Newtonsoft.Json;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    internal sealed class RemoteLevelManifestParser
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Culture = CultureInfo.InvariantCulture,
            MissingMemberHandling = MissingMemberHandling.Error
        };

        public bool TryParse(
            string json,
            out RemoteLevelManifestDto manifest)
        {
            manifest = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                manifest = JsonConvert.DeserializeObject<RemoteLevelManifestDto>(
                    json,
                    _serializerSettings);
                return manifest != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
