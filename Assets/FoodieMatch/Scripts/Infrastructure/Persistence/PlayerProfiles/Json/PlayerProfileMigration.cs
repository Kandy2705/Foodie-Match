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
                    if (version == 3)
                    {
                        profileObject["adsRemoved"] = false;
                        profileObject["unlimitedHeartEndUnixSeconds"] = 0;
                        version = 4;
                        profileObject["schemaVersion"] = version;
                    }
                    else if (version == 4)
                    {
                        if (profileObject["playerName"] == null)
                        {
                            profileObject["playerName"] = "Kandy";
                        }

                        if (profileObject["avatarId"] == null)
                        {
                            profileObject["avatarId"] = "avatar_01";
                        }

                        if (profileObject["frameId"] == null)
                        {
                            profileObject["frameId"] = "frame_01";
                        }

                        version = 5;
                        profileObject["schemaVersion"] = version;
                    }
                    else
                    {
                        migratedJson = null;
                        errorMessage =
                            $"Player profile schema version {version} cannot be migrated.";
                        return false;
                    }
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
