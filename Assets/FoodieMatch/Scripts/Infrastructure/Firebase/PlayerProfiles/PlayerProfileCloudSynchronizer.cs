using System;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Authentication;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json;

namespace FoodieMatch.Infrastructure.Firebase.PlayerProfiles
{
    public sealed class PlayerProfileCloudSynchronizer
    {
        private readonly object _queueLock = new();
        private readonly IPlayerProfileRepository _localRepository;
        private readonly IPlayerProfileRepository _cloudRepository;
        private readonly IPlayerIdentityService _playerIdentityService;
        private readonly PlayerPrefsPlayerProfileSyncMetadataStore _metadataStore;
        private readonly PlayerProfileSession _profileSession;
        private readonly PlayerProfileMapper _profileMapper = new();
        private readonly PlayerProfileJsonParser _jsonParser = new();

        private PlayerProfileRecord _pendingUpload;
        private bool _isUploadRunning;
        private bool _runtimeUploadsEnabled;

        internal PlayerProfileCloudSynchronizer(
            IPlayerProfileRepository localRepository,
            IPlayerProfileRepository cloudRepository,
            IPlayerIdentityService playerIdentityService,
            PlayerPrefsPlayerProfileSyncMetadataStore metadataStore,
            PlayerProfileSession profileSession)
        {
            _localRepository = localRepository ??
                throw new ArgumentNullException(nameof(localRepository));
            _cloudRepository = cloudRepository ??
                throw new ArgumentNullException(nameof(cloudRepository));
            _playerIdentityService = playerIdentityService ??
                throw new ArgumentNullException(nameof(playerIdentityService));
            _metadataStore = metadataStore ??
                throw new ArgumentNullException(nameof(metadataStore));
            _profileSession = profileSession ??
                throw new ArgumentNullException(nameof(profileSession));
        }

        public async Task<PlayerProfileRecord> SynchronizeAsync(
            CancellationToken cancellationToken)
        {
            PlayerProfileRecord localRecord = _profileSession.CurrentRecord;

            if (!_playerIdentityService.IsAuthenticated)
            {
                return localRecord;
            }

            PlayerProfileLoadResult cloudLoadResult =
                await _cloudRepository.LoadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            bool synchronized = false;

            if (cloudLoadResult.Status == PlayerProfileLoadStatus.NotFound)
            {
                synchronized = await UploadLocalAsync(
                    localRecord,
                    expectedCloudRevision: 0,
                    cancellationToken);
            }
            else if (cloudLoadResult.IsSuccess)
            {
                PlayerProfileRecord cloudRecord = cloudLoadResult.Record;
                bool hasMetadata = _metadataStore.TryLoad(
                    _playerIdentityService.PlayerId,
                    out PlayerProfileSyncMetadata metadata);

                if (!hasMetadata)
                {
                    synchronized = await ApplyCloudAsync(
                        localRecord,
                        cloudRecord,
                        cancellationToken);
                }
                else
                {
                    bool localChanged =
                        localRecord.Revision != metadata.LocalRevision;
                    bool cloudChanged =
                        cloudRecord.Revision != metadata.CloudRevision;

                    if (!localChanged && !cloudChanged)
                    {
                        synchronized = ProfilesMatch(localRecord, cloudRecord);

                        if (!synchronized)
                        {
                            synchronized = await UploadLocalAsync(
                                localRecord,
                                cloudRecord.Revision,
                                cancellationToken);
                        }
                    }
                    else if (!localChanged && cloudChanged)
                    {
                        synchronized = await ApplyCloudAsync(
                            localRecord,
                            cloudRecord,
                            cancellationToken);
                    }
                    else if (localChanged && !cloudChanged)
                    {
                        synchronized = await UploadLocalAsync(
                            localRecord,
                            cloudRecord.Revision,
                            cancellationToken);
                    }
                    else if (ProfilesMatch(localRecord, cloudRecord))
                    {
                        SaveMetadata(
                            localRecord.Revision,
                            cloudRecord.Revision);
                        synchronized = true;
                    }
                    else
                    {
                        synchronized = await UploadLocalAsync(
                            localRecord,
                            cloudRecord.Revision,
                            cancellationToken);
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (synchronized)
            {
                EnableRuntimeUploads();
            }

            return _profileSession.CurrentRecord;
        }

        public void QueueUpload(PlayerProfileRecord localRecord)
        {
            if (localRecord == null)
            {
                throw new ArgumentNullException(nameof(localRecord));
            }

            lock (_queueLock)
            {
                if (!_runtimeUploadsEnabled &&
                    (!_playerIdentityService.IsAuthenticated ||
                     !_metadataStore.TryLoad(
                         _playerIdentityService.PlayerId,
                         out _)))
                {
                    return;
                }

                _runtimeUploadsEnabled = true;
                _pendingUpload = localRecord;

                if (!_isUploadRunning)
                {
                    _isUploadRunning = true;
                    _ = UploadPendingAsync();
                }
            }
        }

        private async Task<bool> ApplyCloudAsync(
            PlayerProfileRecord localRecord,
            PlayerProfileRecord cloudRecord,
            CancellationToken cancellationToken)
        {
            PlayerProfileSaveResult localSaveResult =
                await _localRepository.SaveAsync(
                    cloudRecord.Profile,
                    localRecord.Revision,
                    cancellationToken);

            if (!localSaveResult.IsSuccess)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            _profileSession.ReplaceCurrentRecord(localSaveResult.Record);
            SaveMetadata(
                localSaveResult.Record.Revision,
                cloudRecord.Revision);
            return true;
        }

        private async Task<bool> UploadLocalAsync(
            PlayerProfileRecord localRecord,
            long expectedCloudRevision,
            CancellationToken cancellationToken)
        {
            PlayerProfileSaveResult cloudSaveResult =
                await _cloudRepository.SaveAsync(
                    localRecord.Profile,
                    expectedCloudRevision,
                    cancellationToken);

            if (cloudSaveResult.Status == PlayerProfileSaveStatus.Conflict)
            {
                cloudSaveResult = await _cloudRepository.SaveAsync(
                    localRecord.Profile,
                    cloudSaveResult.CurrentRevision.Value,
                    cancellationToken);
            }

            if (!cloudSaveResult.IsSuccess)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            SaveMetadata(
                localRecord.Revision,
                cloudSaveResult.Record.Revision);
            return true;
        }

        private void EnableRuntimeUploads()
        {
            lock (_queueLock)
            {
                _pendingUpload = null;
                _runtimeUploadsEnabled = true;
            }
        }

        private async Task UploadPendingAsync()
        {
            try
            {
                while (true)
                {
                    PlayerProfileRecord localRecord;

                    lock (_queueLock)
                    {
                        localRecord = _pendingUpload;
                        _pendingUpload = null;
                    }

                    if (localRecord == null)
                    {
                        return;
                    }

                    if (!_metadataStore.TryLoad(
                            _playerIdentityService.PlayerId,
                            out PlayerProfileSyncMetadata metadata))
                    {
                        return;
                    }

                    await UploadLocalAsync(
                        localRecord,
                        metadata.CloudRevision,
                        CancellationToken.None);
                }
            }
            finally
            {
                bool restartUpload;

                lock (_queueLock)
                {
                    _isUploadRunning = false;
                    restartUpload = _pendingUpload != null;

                    if (restartUpload)
                    {
                        _isUploadRunning = true;
                    }
                }

                if (restartUpload)
                {
                    _ = UploadPendingAsync();
                }
            }
        }

        private bool ProfilesMatch(
            PlayerProfileRecord localRecord,
            PlayerProfileRecord cloudRecord)
        {
            return TrySerializeProfile(localRecord, out string localJson) &&
                   TrySerializeProfile(cloudRecord, out string cloudJson) &&
                   string.Equals(localJson, cloudJson, StringComparison.Ordinal);
        }

        private bool TrySerializeProfile(
            PlayerProfileRecord record,
            out string json)
        {
            return _jsonParser.TrySerialize(
                _profileMapper.MapToDto(record.Profile, revision: 0),
                out json,
                out _);
        }

        private void SaveMetadata(
            long localRevision,
            long cloudRevision)
        {
            _metadataStore.Save(
                _playerIdentityService.PlayerId,
                localRevision,
                cloudRevision);
        }
    }
}
