using System;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileLoadResult
    {
        private PlayerProfileLoadResult(
            PlayerProfileLoadStatus status,
            PlayerProfileRecord record,
            string errorMessage,
            int? storedSchemaVersion)
        {
            Status = status;
            Record = record;
            ErrorMessage = errorMessage;
            StoredSchemaVersion = storedSchemaVersion;
        }

        public PlayerProfileLoadStatus Status { get; }

        public PlayerProfileRecord Record { get; }

        public string ErrorMessage { get; }

        public int? StoredSchemaVersion { get; }

        public bool IsSuccess => Status == PlayerProfileLoadStatus.Success;

        public static PlayerProfileLoadResult Succeeded(PlayerProfileRecord record)
        {
            return new PlayerProfileLoadResult(
                PlayerProfileLoadStatus.Success,
                record ?? throw new ArgumentNullException(nameof(record)),
                errorMessage: null,
                storedSchemaVersion: null);
        }

        public static PlayerProfileLoadResult NotFound()
        {
            return new PlayerProfileLoadResult(
                PlayerProfileLoadStatus.NotFound,
                record: null,
                errorMessage: null,
                storedSchemaVersion: null);
        }

        public static PlayerProfileLoadResult InvalidData(string errorMessage)
        {
            return CreateFailure(
                PlayerProfileLoadStatus.InvalidData,
                errorMessage,
                storedSchemaVersion: null);
        }

        public static PlayerProfileLoadResult UnsupportedVersion(int storedSchemaVersion)
        {
            return CreateFailure(
                PlayerProfileLoadStatus.UnsupportedVersion,
                $"Player profile schema version {storedSchemaVersion} is not supported.",
                storedSchemaVersion);
        }

        public static PlayerProfileLoadResult Failed(string errorMessage)
        {
            return CreateFailure(
                PlayerProfileLoadStatus.Failed,
                errorMessage,
                storedSchemaVersion: null);
        }

        private static PlayerProfileLoadResult CreateFailure(
            PlayerProfileLoadStatus status,
            string errorMessage,
            int? storedSchemaVersion)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException(
                    "An error message is required.",
                    nameof(errorMessage));
            }

            return new PlayerProfileLoadResult(
                status,
                record: null,
                errorMessage,
                storedSchemaVersion);
        }
    }
}
