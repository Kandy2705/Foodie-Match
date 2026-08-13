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
