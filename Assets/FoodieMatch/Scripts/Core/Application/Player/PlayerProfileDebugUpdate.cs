using System;

namespace FoodieMatch.Core.Application.Player
{
    public readonly struct PlayerProfileDebugUpdate
    {
        public PlayerProfileDebugUpdate(
            int currentLevelNumber,
            long coinBalance,
            int heartCount,
            int plateBoosterCount,
            int storageBoosterCount,
            int swapBoosterCount,
            int fridgeBoosterCount)
        {
            if (currentLevelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevelNumber));
            }

            if (coinBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coinBalance));
            }

            if (heartCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(heartCount));
            }

            ValidateBoosterCount(plateBoosterCount, nameof(plateBoosterCount));
            ValidateBoosterCount(storageBoosterCount, nameof(storageBoosterCount));
            ValidateBoosterCount(swapBoosterCount, nameof(swapBoosterCount));
            ValidateBoosterCount(fridgeBoosterCount, nameof(fridgeBoosterCount));

            CurrentLevelNumber = currentLevelNumber;
            CoinBalance = coinBalance;
            HeartCount = heartCount;
            PlateBoosterCount = plateBoosterCount;
            StorageBoosterCount = storageBoosterCount;
            SwapBoosterCount = swapBoosterCount;
            FridgeBoosterCount = fridgeBoosterCount;
        }

        public int CurrentLevelNumber { get; }

        public long CoinBalance { get; }

        public int HeartCount { get; }

        public int PlateBoosterCount { get; }

        public int StorageBoosterCount { get; }

        public int SwapBoosterCount { get; }

        public int FridgeBoosterCount { get; }

        private static void ValidateBoosterCount(int count, string parameterName)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
