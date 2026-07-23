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
            long coinBalance,
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

            if (coinBalance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coinBalance),
                    coinBalance,
                    "Coin balance cannot be negative.");
            }

            CurrentLevelNumber = currentLevelNumber;
            CoinBalance = coinBalance;
            _boosterCounts = CopyBoosterCounts(boosterCounts);
            _seenBoosterGuideSet = CopySeenBoosterGuides(seenBoosterGuides);
            _seenBoosterGuides = new ReadOnlyCollection<BoosterType>(
                new List<BoosterType>(_seenBoosterGuideSet));
        }

        public int CurrentLevelNumber { get; }

        public long CoinBalance { get; }

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

        public PlayerProfile WithCurrentLevelNumber(int currentLevelNumber)
        {
            if (currentLevelNumber == CurrentLevelNumber)
            {
                return this;
            }

            return new PlayerProfile(
                currentLevelNumber,
                CoinBalance,
                _boosterCounts,
                _seenBoosterGuides);
        }

        public PlayerProfile WithCoinBalance(long coinBalance)
        {
            if (coinBalance == CoinBalance)
            {
                return this;
            }

            return new PlayerProfile(
                CurrentLevelNumber,
                coinBalance,
                _boosterCounts,
                _seenBoosterGuides);
        }

        public PlayerProfile WithBoosterCount(
            BoosterType boosterType,
            int count)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "Booster count cannot be negative.");
            }

            if (GetBoosterCount(boosterType) == count)
            {
                return this;
            }

            Dictionary<BoosterType, int> boosterCounts = new(_boosterCounts)
            {
                [boosterType] = count
            };

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                boosterCounts,
                _seenBoosterGuides);
        }

        public PlayerProfile WithSeenBoosterGuide(BoosterType boosterType)
        {
            ValidateBoosterType(boosterType, nameof(boosterType));

            if (HasSeenBoosterGuide(boosterType))
            {
                return this;
            }

            List<BoosterType> seenBoosterGuides = new(_seenBoosterGuides)
            {
                boosterType
            };

            return new PlayerProfile(
                CurrentLevelNumber,
                CoinBalance,
                _boosterCounts,
                seenBoosterGuides);
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
