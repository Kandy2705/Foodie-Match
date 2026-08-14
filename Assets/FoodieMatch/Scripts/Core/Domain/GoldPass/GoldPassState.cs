using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.GoldPass
{
    public sealed class GoldPassState
    {
        private readonly ReadOnlyCollection<int> _claimedFreeMilestoneLevels;
        private readonly ReadOnlyCollection<int> _claimedSeasonMilestoneLevels;

        public GoldPassState(
            string seasonId,
            int spoonCount,
            bool isSeasonPassPurchased,
            IReadOnlyCollection<int> claimedFreeMilestoneLevels,
            IReadOnlyCollection<int> claimedSeasonMilestoneLevels)
        {
            if (seasonId == null)
            {
                throw new ArgumentNullException(nameof(seasonId));
            }

            if (spoonCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spoonCount));
            }

            SeasonId = seasonId;
            SpoonCount = spoonCount;
            IsSeasonPassPurchased = isSeasonPassPurchased;
            _claimedFreeMilestoneLevels = CopyClaimedLevels(
                claimedFreeMilestoneLevels,
                nameof(claimedFreeMilestoneLevels));
            _claimedSeasonMilestoneLevels = CopyClaimedLevels(
                claimedSeasonMilestoneLevels,
                nameof(claimedSeasonMilestoneLevels));
        }

        public string SeasonId { get; }

        public int SpoonCount { get; }

        public bool IsSeasonPassPurchased { get; }

        public IReadOnlyCollection<int> ClaimedFreeMilestoneLevels =>
            _claimedFreeMilestoneLevels;

        public IReadOnlyCollection<int> ClaimedSeasonMilestoneLevels =>
            _claimedSeasonMilestoneLevels;

        public static GoldPassState Empty { get; } = new(
            string.Empty,
            0,
            false,
            Array.Empty<int>(),
            Array.Empty<int>());

        public static GoldPassState CreateForSeason(string seasonId)
        {
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                throw new ArgumentException(
                    "Gold Pass season id is required.",
                    nameof(seasonId));
            }

            return new GoldPassState(
                seasonId,
                0,
                false,
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        public GoldPassState WithAddedSpoons(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return new GoldPassState(
                SeasonId,
                checked(SpoonCount + amount),
                IsSeasonPassPurchased,
                _claimedFreeMilestoneLevels,
                _claimedSeasonMilestoneLevels);
        }

        public GoldPassState WithSpoonCount(int spoonCount)
        {
            if (spoonCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spoonCount));
            }

            return new GoldPassState(
                SeasonId,
                spoonCount,
                IsSeasonPassPurchased,
                _claimedFreeMilestoneLevels,
                _claimedSeasonMilestoneLevels);
        }

        public GoldPassState WithSeasonPassPurchaseStatus(bool isPurchased)
        {
            return new GoldPassState(
                SeasonId,
                SpoonCount,
                isPurchased,
                _claimedFreeMilestoneLevels,
                _claimedSeasonMilestoneLevels);
        }

        public GoldPassState WithoutClaimedMilestones()
        {
            return new GoldPassState(
                SeasonId,
                SpoonCount,
                IsSeasonPassPurchased,
                Array.Empty<int>(),
                Array.Empty<int>());
        }

        public GoldPassState WithSeasonPassPurchased()
        {
            if (IsSeasonPassPurchased)
            {
                return this;
            }

            return new GoldPassState(
                SeasonId,
                SpoonCount,
                true,
                _claimedFreeMilestoneLevels,
                _claimedSeasonMilestoneLevels);
        }

        public bool HasClaimedFreeMilestone(int level)
        {
            return _claimedFreeMilestoneLevels.Contains(level);
        }

        public bool HasClaimedSeasonMilestone(int level)
        {
            return _claimedSeasonMilestoneLevels.Contains(level);
        }

        public GoldPassState WithClaimedFreeMilestone(int level)
        {
            if (HasClaimedFreeMilestone(level))
            {
                return this;
            }

            List<int> claimedLevels = new(_claimedFreeMilestoneLevels)
            {
                level
            };

            return new GoldPassState(
                SeasonId,
                SpoonCount,
                IsSeasonPassPurchased,
                claimedLevels,
                _claimedSeasonMilestoneLevels);
        }

        public GoldPassState WithClaimedSeasonMilestone(int level)
        {
            if (HasClaimedSeasonMilestone(level))
            {
                return this;
            }

            List<int> claimedLevels = new(_claimedSeasonMilestoneLevels)
            {
                level
            };

            return new GoldPassState(
                SeasonId,
                SpoonCount,
                IsSeasonPassPurchased,
                _claimedFreeMilestoneLevels,
                claimedLevels);
        }

        private static ReadOnlyCollection<int> CopyClaimedLevels(
            IReadOnlyCollection<int> claimedLevels,
            string parameterName)
        {
            if (claimedLevels == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            HashSet<int> uniqueLevels = new();

            foreach (int level in claimedLevels)
            {
                if (level < 0)
                {
                    throw new ArgumentOutOfRangeException(parameterName);
                }

                if (!uniqueLevels.Add(level))
                {
                    throw new ArgumentException(
                        $"Claimed Gold Pass milestone {level} is duplicated.",
                        parameterName);
                }
            }

            List<int> copiedLevels = new(uniqueLevels);
            copiedLevels.Sort();
            return new ReadOnlyCollection<int>(copiedLevels);
        }
    }
}
