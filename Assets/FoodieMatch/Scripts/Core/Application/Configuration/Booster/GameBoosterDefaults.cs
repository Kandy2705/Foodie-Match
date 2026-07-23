using System.Collections.Generic;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.Booster
{
    public static class GameBoosterDefaults
    {
        private const int DefaultPlateUnlockLevel = 1;
        private const int DefaultStorageUnlockLevel = 1;
        private const int DefaultSwapUnlockLevel = 5;
        private const int DefaultFridgeUnlockLevel = 2;
        private const int DefaultBoxUnlockLevel = 1;

        public static GameBoosterConfigSnapshot CreateSnapshot()
        {
            Dictionary<BoosterType, int> unlockLevels = new()
            {
                [BoosterType.Plate] = DefaultPlateUnlockLevel,
                [BoosterType.Storage] = DefaultStorageUnlockLevel,
                [BoosterType.Swap] = DefaultSwapUnlockLevel,
                [BoosterType.Fridge] = DefaultFridgeUnlockLevel,
                [BoosterType.Box] = DefaultBoxUnlockLevel
            };

            return new GameBoosterConfigSnapshot(unlockLevels);
        }
    }
}
