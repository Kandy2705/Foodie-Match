using System;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Player;
using FoodieMatch.Core.Infrastructure.Save;
using FoodieMatch.Infrastructure.Persistence.PlayerProfiles.Json;

namespace FoodieMatch.Infrastructure.Persistence.PlayerProfiles
{
    public sealed class PlayerPrefsPlayerProfileRepository : IPlayerProfileRepository
    {
        private const string PlayerProfileKey = "PlayerProfile";

        private readonly ISaveService _saveService;
        private readonly PlayerProfileJsonParser _jsonParser = new();
        private readonly PlayerProfileMapper _mapper = new();

        public PlayerPrefsPlayerProfileRepository(ISaveService saveService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public Task<PlayerProfileLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<PlayerProfileLoadResult>(cancellationToken);
            }

            return Task.FromResult(Load());
        }

        public Task<PlayerProfileSaveResult> SaveAsync(
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

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<PlayerProfileSaveResult>(cancellationToken);
            }

            return Task.FromResult(Save(profile, expectedRevision));
        }

        private PlayerProfileLoadResult Load()
        {
            try
            {
                if (!_saveService.HasKey(PlayerProfileKey))
                {
                    return PlayerProfileLoadResult.NotFound();
                }

                string json = _saveService.GetString(PlayerProfileKey, defaultValue: null);

                if (!_jsonParser.TryReadSchemaVersion(
                        json,
                        out int schemaVersion,
                        out string schemaError))
                {
                    return PlayerProfileLoadResult.InvalidData(schemaError);
                }

                if (schemaVersion > PlayerProfileDataVersions.Current)
                {
                    return PlayerProfileLoadResult.UnsupportedVersion(schemaVersion);
                }

                if (schemaVersion != PlayerProfileDataVersions.Current)
                {
                    return PlayerProfileLoadResult.InvalidData(
                        $"Player profile schema version {schemaVersion} is invalid.");
                }

                if (!_jsonParser.TryDeserialize(
                        json,
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

                return PlayerProfileLoadResult.Succeeded(record);
            }
            catch (Exception exception)
            {
                return PlayerProfileLoadResult.Failed(exception.Message);
            }
        }

        private PlayerProfileSaveResult Save(
            PlayerProfile profile,
            long expectedRevision)
        {
            PlayerProfileLoadResult loadResult = Load();
            long currentRevision;

            if (loadResult.Status == PlayerProfileLoadStatus.NotFound)
            {
                currentRevision = 0;
            }
            else if (loadResult.IsSuccess)
            {
                currentRevision = loadResult.Record.Revision;
            }
            else
            {
                return PlayerProfileSaveResult.Failed(
                    $"Existing player profile could not be read: " +
                    $"{loadResult.ErrorMessage}");
            }

            if (currentRevision != expectedRevision)
            {
                return PlayerProfileSaveResult.Conflict(currentRevision);
            }

            if (currentRevision == long.MaxValue)
            {
                return PlayerProfileSaveResult.Failed(
                    "Player profile revision cannot be increased.");
            }

            long nextRevision = currentRevision + 1;
            PlayerProfileDto profileDto = _mapper.MapToDto(profile, nextRevision);

            if (!_jsonParser.TrySerialize(
                    profileDto,
                    out string json,
                    out string serializationError))
            {
                return PlayerProfileSaveResult.Failed(serializationError);
            }

            try
            {
                _saveService.SetString(PlayerProfileKey, json);
                _saveService.Save();

                return PlayerProfileSaveResult.Succeeded(
                    new PlayerProfileRecord(profile, nextRevision));
            }
            catch (Exception exception)
            {
                return PlayerProfileSaveResult.Failed(exception.Message);
            }
        }
    }
}
