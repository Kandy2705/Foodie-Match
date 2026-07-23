using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Core.Domain.Booster;

namespace FoodieMatch.Core.Application.Configuration.Booster
{
    public sealed class GameBoosterConfigSnapshot : IGameBoosterConfig
    {
        private static readonly BoosterType[] AllBoosterTypes =
            (BoosterType[])Enum.GetValues(typeof(BoosterType));

        private readonly ReadOnlyDictionary<BoosterType, int> _unlockLevels;

        public GameBoosterConfigSnapshot(
            IReadOnlyDictionary<BoosterType, int> unlockLevels)
        {
            _unlockLevels = CopyUnlockLevels(unlockLevels);
        }

        public int GetUnlockLevel(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType);
            return _unlockLevels[boosterType];
        }

        private static ReadOnlyDictionary<BoosterType, int> CopyUnlockLevels(
            IReadOnlyDictionary<BoosterType, int> unlockLevels)
        {
            if (unlockLevels == null)
            {
                throw new ArgumentNullException(nameof(unlockLevels));
            }

            Dictionary<BoosterType, int> copiedUnlockLevels = new();

            foreach (KeyValuePair<BoosterType, int> unlockLevel in unlockLevels)
            {
                ValidateBoosterType(unlockLevel.Key);

                if (unlockLevel.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(unlockLevels),
                        unlockLevel.Value,
                        "Booster unlock level must be greater than zero.");
                }

                copiedUnlockLevels.Add(unlockLevel.Key, unlockLevel.Value);
            }

            foreach (BoosterType boosterType in AllBoosterTypes)
            {
                if (!copiedUnlockLevels.ContainsKey(boosterType))
                {
                    throw new ArgumentException(
                        $"Unlock level for {boosterType} is missing.",
                        nameof(unlockLevels));
                }
            }

            return new ReadOnlyDictionary<BoosterType, int>(
                copiedUnlockLevels);
        }

        private static void ValidateBoosterType(BoosterType boosterType)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(boosterType),
                    boosterType,
                    "Booster type is not defined.");
            }
        }
    }
}
