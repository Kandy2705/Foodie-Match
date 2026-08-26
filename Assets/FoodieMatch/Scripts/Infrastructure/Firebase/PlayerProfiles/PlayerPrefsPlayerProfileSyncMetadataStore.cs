using System;
using System.Globalization;
using FoodieMatch.Infrastructure.Persistence.Save;

namespace FoodieMatch.Infrastructure.Firebase.PlayerProfiles
{
    internal sealed class PlayerPrefsPlayerProfileSyncMetadataStore
    {
        private const string PlayerIdKey = "PlayerProfile.Sync.PlayerId";
        private const string LocalRevisionKey = "PlayerProfile.Sync.LocalRevision";
        private const string CloudRevisionKey = "PlayerProfile.Sync.CloudRevision";

        private readonly ISaveService _saveService;

        public PlayerPrefsPlayerProfileSyncMetadataStore(ISaveService saveService)
        {
            _saveService = saveService ??
                throw new ArgumentNullException(nameof(saveService));
        }

        public bool TryLoad(
            string playerId,
            out PlayerProfileSyncMetadata metadata)
        {
            metadata = null;

            if (_saveService.GetString(PlayerIdKey, string.Empty) != playerId ||
                !TryLoadRevision(LocalRevisionKey, out long localRevision) ||
                !TryLoadRevision(CloudRevisionKey, out long cloudRevision))
            {
                return false;
            }

            metadata = new PlayerProfileSyncMetadata(
                localRevision,
                cloudRevision);
            return true;
        }

        public void Save(
            string playerId,
            long localRevision,
            long cloudRevision)
        {
            _saveService.SetString(PlayerIdKey, playerId);
            _saveService.SetString(
                LocalRevisionKey,
                localRevision.ToString(CultureInfo.InvariantCulture));
            _saveService.SetString(
                CloudRevisionKey,
                cloudRevision.ToString(CultureInfo.InvariantCulture));
            _saveService.Save();
        }

        private bool TryLoadRevision(string key, out long revision)
        {
            return long.TryParse(
                       _saveService.GetString(key, string.Empty),
                       NumberStyles.None,
                       CultureInfo.InvariantCulture,
                       out revision) &&
                   revision >= 0;
        }
    }

    internal sealed class PlayerProfileSyncMetadata
    {
        public PlayerProfileSyncMetadata(
            long localRevision,
            long cloudRevision)
        {
            LocalRevision = localRevision;
            CloudRevision = cloudRevision;
        }

        public long LocalRevision { get; }

        public long CloudRevision { get; }
    }
}
