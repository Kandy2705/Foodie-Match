using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Firestore;
using FoodieMatch.Core.Application.Authentication;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Player;
using FoodieMatch.Infrastructure.Persistence.PlayerProfiles;
using FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json;

namespace FoodieMatch.Infrastructure.Firebase.PlayerProfiles
{
    public sealed class FirestorePlayerProfileRepository :
        IPlayerProfileRepository
    {
        private const string CollectionName = "playerProfiles";

        private readonly IPlayerIdentityService _playerIdentityService;
        private readonly PlayerProfileJsonParser _jsonParser = new();
        private readonly PlayerProfileMapper _mapper = new();
        private readonly PlayerProfileMigration _migration = new();

        private FirebaseFirestore _firestore;

        public FirestorePlayerProfileRepository(
            IPlayerIdentityService playerIdentityService)
        {
            _playerIdentityService = playerIdentityService;
        }

        public async Task<PlayerProfileLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_playerIdentityService.IsAuthenticated)
            {
                return PlayerProfileLoadResult.Failed(
                    "Player identity is not authenticated.");
            }

            try
            {
                DocumentSnapshot snapshot = await GetDocumentReference()
                    .GetSnapshotAsync(Source.Server);
                cancellationToken.ThrowIfCancellationRequested();

                if (!snapshot.Exists)
                {
                    return PlayerProfileLoadResult.NotFound();
                }

                return MapSnapshot(snapshot);
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return PlayerProfileLoadResult.Failed(exception.Message);
            }
        }

        public async Task<PlayerProfileSaveResult> SaveAsync(
            PlayerProfile profile,
            long expectedRevision,
            CancellationToken cancellationToken = default)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (expectedRevision < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedRevision),
                    expectedRevision,
                    "Expected revision cannot be negative.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!_playerIdentityService.IsAuthenticated)
            {
                return PlayerProfileSaveResult.Failed(
                    "Player identity is not authenticated.");
            }

            if (expectedRevision == long.MaxValue)
            {
                return PlayerProfileSaveResult.Failed(
                    "Player profile revision cannot be increased.");
            }

            long nextRevision = expectedRevision + 1;
            PlayerProfileDto profileDto = _mapper.MapToDto(
                profile,
                nextRevision);

            if (!_jsonParser.TrySerialize(
                    profileDto,
                    out string profileJson,
                    out string serializationError))
            {
                return PlayerProfileSaveResult.Failed(serializationError);
            }

            try
            {
                DocumentReference documentReference = GetDocumentReference();
                long? conflictingRevision = await GetFirestore()
                    .RunTransactionAsync<long?>(
                        async transaction =>
                        {
                            DocumentSnapshot snapshot =
                                await transaction.GetSnapshotAsync(
                                    documentReference);
                            long currentRevision = 0;

                            if (snapshot.Exists &&
                                !FirestorePlayerProfileDocument.TryReadRevision(
                                    snapshot,
                                    out currentRevision))
                            {
                                throw new InvalidOperationException(
                                    "Cloud player profile revision is invalid.");
                            }

                            if (currentRevision != expectedRevision)
                            {
                                return currentRevision;
                            }

                            Dictionary<string, object> data =
                                FirestorePlayerProfileDocument.CreateWriteData(
                                    PlayerProfileDataVersions.Current,
                                    nextRevision,
                                    profileJson,
                                    includeCreatedAt: !snapshot.Exists);
                            transaction.Set(
                                documentReference,
                                data,
                                SetOptions.MergeAll);
                            return null;
                        });
                cancellationToken.ThrowIfCancellationRequested();

                if (conflictingRevision.HasValue)
                {
                    return PlayerProfileSaveResult.Conflict(
                        conflictingRevision.Value);
                }

                return PlayerProfileSaveResult.Succeeded(
                    new PlayerProfileRecord(profile, nextRevision));
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return PlayerProfileSaveResult.Failed(exception.Message);
            }
        }

        private PlayerProfileLoadResult MapSnapshot(
            DocumentSnapshot snapshot)
        {
            if (!FirestorePlayerProfileDocument.TryRead(
                    snapshot,
                    out FirestorePlayerProfileDocument document,
                    out string documentError))
            {
                return PlayerProfileLoadResult.InvalidData(documentError);
            }

            if (document.SchemaVersion > PlayerProfileDataVersions.Current)
            {
                return PlayerProfileLoadResult.UnsupportedVersion(
                    document.SchemaVersion);
            }

            if (!_jsonParser.TryReadSchemaVersion(
                    document.ProfileJson,
                    out int jsonSchemaVersion,
                    out string schemaError))
            {
                return PlayerProfileLoadResult.InvalidData(schemaError);
            }

            if (jsonSchemaVersion != document.SchemaVersion)
            {
                return PlayerProfileLoadResult.InvalidData(
                    "Cloud player profile schema versions do not match.");
            }

            string profileJson = document.ProfileJson;

            if (jsonSchemaVersion != PlayerProfileDataVersions.Current &&
                !_migration.TryMigrate(
                    profileJson,
                    jsonSchemaVersion,
                    out profileJson,
                    out string migrationError))
            {
                return PlayerProfileLoadResult.InvalidData(migrationError);
            }

            if (!_jsonParser.TryDeserialize(
                    profileJson,
                    out PlayerProfileDto profileDto,
                    out string parseError))
            {
                return PlayerProfileLoadResult.InvalidData(parseError);
            }

            if (!_mapper.TryMapToRecord(
                    profileDto,
                    out PlayerProfileRecord record,
                    out string mappingError))
            {
                return PlayerProfileLoadResult.InvalidData(mappingError);
            }

            if (record.Revision != document.Revision)
            {
                return PlayerProfileLoadResult.InvalidData(
                    "Cloud player profile revisions do not match.");
            }

            return PlayerProfileLoadResult.Succeeded(record);
        }

        private DocumentReference GetDocumentReference()
        {
            return GetFirestore()
                .Collection(CollectionName)
                .Document(_playerIdentityService.PlayerId);
        }

        private FirebaseFirestore GetFirestore()
        {
            return _firestore ??= FirebaseFirestore.DefaultInstance;
        }
    }
}
