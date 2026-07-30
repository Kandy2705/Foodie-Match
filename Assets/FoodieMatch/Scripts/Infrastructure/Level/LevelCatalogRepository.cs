using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level
{
    public sealed class LevelCatalogRepository : ILevelCatalogRepository
    {
        private readonly LevelCatalog _catalog;
        private readonly Dictionary<int, int> _levelIndices = new();

        public LevelCatalogRepository(LevelCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            for (int i = 0; i < _catalog.OrderedLevels.Count; i++)
            {
                _levelIndices.Add(
                    _catalog.OrderedLevels[i].LevelNumber,
                    i);
            }
        }

        public bool TryGetLevelSummary(
            int levelNumber,
            out LevelSummary summary)
        {
            if (_levelIndices.TryGetValue(levelNumber, out int levelIndex))
            {
                summary = _catalog.OrderedLevels[levelIndex];
                return true;
            }

            summary = default;
            return false;
        }

        public bool TryGetFirstLevelSummary(out LevelSummary summary)
        {
            summary = _catalog.OrderedLevels[0];
            return true;
        }

        public bool TryGetNextLevelSummary(
            int currentLevelNumber,
            out LevelSummary summary)
        {
            if (!_levelIndices.TryGetValue(
                    currentLevelNumber,
                    out int currentLevelIndex))
            {
                summary = default;
                return false;
            }

            int nextLevelIndex = currentLevelIndex + 1;

            if (nextLevelIndex >= _catalog.OrderedLevels.Count)
            {
                summary = default;
                return false;
            }

            summary = _catalog.OrderedLevels[nextLevelIndex];
            return true;
        }
    }
}
