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
                    switch (version)
                    {
                        case 3:
                            profileObject["adsRemoved"] = false;
                            profileObject["unlimitedHeartEndUnixSeconds"] = 0;
                            version = 4;
                            break;

                        case 4:
                        case 5:
                            AddMissingVersionSixFields(profileObject);
                            version = 6;
                            break;

                        default:
                            migratedJson = null;
                            errorMessage =
                                $"Player profile schema version {version} cannot be migrated.";
                            return false;
                    }

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

        private static void AddMissingVersionSixFields(JObject profileObject)
        {
            profileObject["goldPass"] ??= new JObject
            {
                ["seasonId"] = string.Empty,
                ["spoonCount"] = 0,
                ["isSeasonPassPurchased"] = false,
                ["claimedFreeMilestoneLevels"] = new JArray(),
                ["claimedSeasonMilestoneLevels"] = new JArray()
            };
            profileObject["playerName"] ??= "Kandy";
            profileObject["avatarId"] ??= "avatar_01";
            profileObject["frameId"] ??= "frame_01";
        }
    }
}
