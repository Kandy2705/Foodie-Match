using System;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileSaveResult
    {
        private PlayerProfileSaveResult(
            PlayerProfileSaveStatus status,
            PlayerProfileRecord record,
            long? currentRevision,
            string errorMessage)
        {
            Status = status;
            Record = record;
            CurrentRevision = currentRevision;
            ErrorMessage = errorMessage;
        }

        public PlayerProfileSaveStatus Status { get; }

        public PlayerProfileRecord Record { get; }

        public long? CurrentRevision { get; }

        public string ErrorMessage { get; }

        public bool IsSuccess => Status == PlayerProfileSaveStatus.Success;

        public static PlayerProfileSaveResult Succeeded(PlayerProfileRecord record)
        {
            return new PlayerProfileSaveResult(
                PlayerProfileSaveStatus.Success,
                record ?? throw new ArgumentNullException(nameof(record)),
                currentRevision: null,
                errorMessage: null);
        }

        public static PlayerProfileSaveResult Conflict(long currentRevision)
        {
            if (currentRevision < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentRevision),
                    currentRevision,
                    "Revision cannot be negative.");
            }

            return new PlayerProfileSaveResult(
                PlayerProfileSaveStatus.Conflict,
                record: null,
                currentRevision,
                errorMessage: null);
        }

        public static PlayerProfileSaveResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException(
                    "An error message is required.",
                    nameof(errorMessage));
            }

            return new PlayerProfileSaveResult(
                PlayerProfileSaveStatus.Failed,
                record: null,
                currentRevision: null,
                errorMessage);
        }
    }
}
