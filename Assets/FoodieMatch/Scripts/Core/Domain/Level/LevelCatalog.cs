using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class LevelCatalog
    {
        private readonly ReadOnlyCollection<LevelDefinition> _orderedLevels;

        public LevelCatalog(IReadOnlyList<LevelDefinition> orderedLevels)
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

            List<LevelDefinition> copiedLevels = new(orderedLevels);
            _orderedLevels = copiedLevels.AsReadOnly();
        }

        public IReadOnlyList<LevelDefinition> OrderedLevels => _orderedLevels;

        private static void ValidateLevels(IReadOnlyList<LevelDefinition> levels)
        {
            HashSet<int> levelIds = new();

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];

                if (level == null)
                {
                    throw new ArgumentException(
                        "Level catalog cannot contain null.",
                        nameof(levels));
                }

                if (!levelIds.Add(level.Id))
                {
                    throw new ArgumentException(
                        $"Level id {level.Id} is duplicated.",
                        nameof(levels));
                }
            }
        }
    }
}
