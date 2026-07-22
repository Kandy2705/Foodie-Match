using System;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileSession
    {
        private PlayerProfileRecord _currentRecord;

        public bool IsInitialized => _currentRecord != null;

        public PlayerProfileRecord CurrentRecord => _currentRecord ??
            throw new InvalidOperationException("Player profile has not been initialized.");

        public void Initialize(PlayerProfileRecord record)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Player profile has already been initialized.");
            }

            _currentRecord = record ?? throw new ArgumentNullException(nameof(record));
        }

        public void ReplaceCurrentRecord(PlayerProfileRecord record)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Player profile has not been initialized.");
            }

            _currentRecord = record ?? throw new ArgumentNullException(nameof(record));
        }
    }
}
