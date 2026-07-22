using System;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileInitializationResult
    {
        private PlayerProfileInitializationResult(
            PlayerProfileRecord record,
            string errorMessage,
            bool recoveredInvalidData)
        {
            Record = record;
            ErrorMessage = errorMessage;
            RecoveredInvalidData = recoveredInvalidData;
        }

        public PlayerProfileRecord Record { get; }

        public string ErrorMessage { get; }

        public bool RecoveredInvalidData { get; }

        public bool IsSuccess => Record != null;

        public static PlayerProfileInitializationResult Succeeded(
            PlayerProfileRecord record,
            bool recoveredInvalidData)
        {
            return new PlayerProfileInitializationResult(
                record ?? throw new ArgumentNullException(nameof(record)),
                errorMessage: null,
                recoveredInvalidData);
        }

        public static PlayerProfileInitializationResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException(
                    "An error message is required.",
                    nameof(errorMessage));
            }

            return new PlayerProfileInitializationResult(
                record: null,
                errorMessage,
                recoveredInvalidData: false);
        }
    }
}
