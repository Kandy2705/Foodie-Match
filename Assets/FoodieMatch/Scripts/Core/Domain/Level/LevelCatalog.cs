using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class LevelCatalog
    {
        private readonly ReadOnlyCollection<LevelSummary> _orderedLevels;

        public LevelCatalog(IReadOnlyList<LevelSummary> orderedLevels)
        {
            if (orderedLevels == null)
            {
                throw new ArgumentNullException(nameof(orderedLevels));
            }

            if (orderedLevels.Count == 0)
            {
                throw new ArgumentException(
                    "Level catalog must contain at least one level.",
                    nameof(orderedLevels));
            }

            ValidateLevels(orderedLevels);

            List<LevelSummary> copiedLevels = new(orderedLevels);
            _orderedLevels = copiedLevels.AsReadOnly();
        }

        public IReadOnlyList<LevelSummary> OrderedLevels => _orderedLevels;

        private static void ValidateLevels(IReadOnlyList<LevelSummary> levels)
        {
            HashSet<int> levelIds = new();

            for (int i = 0; i < levels.Count; i++)
            {
                LevelSummary level = levels[i];

                if (level.LevelNumber <= 0)
                {
                    throw new ArgumentException(
                        "Level number must be greater than zero.",
                        nameof(levels));
                }

                if (!levelIds.Add(level.LevelNumber))
                {
                    throw new ArgumentException(
                        $"Level number {level.LevelNumber} is duplicated.",
                        nameof(levels));
                }
            }
        }
    }
}
