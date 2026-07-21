using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FoodieMatch.Data.Booster;

namespace FoodieMatch.Core.Domain.Player
{
    public sealed class PlayerProfile
    {
        private readonly ReadOnlyDictionary<BoosterType, int> _boosterCounts;
        private readonly ReadOnlyCollection<BoosterType> _seenBoosterGuides;
        private readonly HashSet<BoosterType> _seenBoosterGuideSet;

        public PlayerProfile(
            int currentLevelNumber,
            IReadOnlyDictionary<BoosterType, int> boosterCounts,
            IReadOnlyCollection<BoosterType> seenBoosterGuides)
        {
            if (currentLevelNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentLevelNumber),
                    currentLevelNumber,
                    "Current level number must be at least 1.");
            }

            CurrentLevelNumber = currentLevelNumber;
            _boosterCounts = CopyBoosterCounts(boosterCounts);
            _seenBoosterGuideSet = CopySeenBoosterGuides(seenBoosterGuides);
            _seenBoosterGuides = new ReadOnlyCollection<BoosterType>(
                new List<BoosterType>(_seenBoosterGuideSet));
        }

        public int CurrentLevelNumber { get; }

        public IReadOnlyDictionary<BoosterType, int> BoosterCounts => _boosterCounts;

        public IReadOnlyCollection<BoosterType> SeenBoosterGuides => _seenBoosterGuides;

        public int GetBoosterCount(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));
            return _boosterCounts.TryGetValue(boosterType, out int count)
                ? count
                : 0;
        }

        public bool HasSeenBoosterGuide(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));
            return _seenBoosterGuideSet.Contains(boosterType);
        }

        private static ReadOnlyDictionary<BoosterType, int> CopyBoosterCounts(
            IReadOnlyDictionary<BoosterType, int> boosterCounts)
        {
            if (boosterCounts == null)
            {
                throw new ArgumentNullException(nameof(boosterCounts));
            }

            Dictionary<BoosterType, int> copiedCounts = new();

            foreach (KeyValuePair<BoosterType, int> boosterCount in boosterCounts)
            {
                ValidateBoosterType(boosterCount.Key, nameof(boosterCounts));

                if (boosterCount.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(boosterCounts),
                        boosterCount.Value,
                        "Booster count cannot be negative.");
                }

                copiedCounts.Add(boosterCount.Key, boosterCount.Value);
            }

            return new ReadOnlyDictionary<BoosterType, int>(copiedCounts);
        }

        private static HashSet<BoosterType> CopySeenBoosterGuides(
            IReadOnlyCollection<BoosterType> seenBoosterGuides)
        {
            if (seenBoosterGuides == null)
            {
                throw new ArgumentNullException(nameof(seenBoosterGuides));
            }

            HashSet<BoosterType> copiedGuides = new();

            foreach (BoosterType boosterType in seenBoosterGuides)
            {
                ValidateBoosterType(boosterType, nameof(seenBoosterGuides));

                if (!copiedGuides.Add(boosterType))
                {
                    throw new ArgumentException(
                        $"Booster guide {boosterType} is duplicated.",
                        nameof(seenBoosterGuides));
                }
            }

            return copiedGuides;
        }

        private static void ValidateBoosterType(
            BoosterType boosterType,
            string parameterName)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    boosterType,
                    "Booster type is not defined.");
            }
        }
    }
}
