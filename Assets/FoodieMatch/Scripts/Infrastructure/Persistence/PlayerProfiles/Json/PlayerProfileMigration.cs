using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json
{
    internal sealed class PlayerProfileMigration
    {
        public bool TryMigrate(
            string json,
            int sourceVersion,
            out string migratedJson,
            out string errorMessage)
        {
            try
            {
                JObject profileObject = JObject.Parse(json);
                int version = sourceVersion;

                while (version < PlayerProfileDataVersions.Current)
                {
                    if (version != 3)
                    {
                        migratedJson = null;
                        errorMessage =
                            $"Player profile schema version {version} cannot be migrated.";
                        return false;
                    }

                    profileObject["adsRemoved"] = false;
                    profileObject["unlimitedHeartEndUnixSeconds"] = 0;
                    version = 4;
                    profileObject["schemaVersion"] = version;
                }

                migratedJson = profileObject.ToString(Formatting.None);
                errorMessage = null;
                return true;
            }
            catch (JsonException exception)
            {
                migratedJson = null;
                errorMessage = exception.Message;
                return false;
            }
        }
    }
}
