using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level
{
    public sealed class LevelCatalogRepository : ILevelCatalogRepository
    {
        private readonly Dictionary<int, LevelSummary> _localLevels = new();
        private readonly Dictionary<int, LevelSummary> _allLevels = new();
        private readonly List<LevelSummary> _orderedLevels = new();

        public LevelCatalogRepository(LevelCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            for (int i = 0; i < catalog.OrderedLevels.Count; i++)
            {
                LevelSummary level = catalog.OrderedLevels[i];
                _localLevels.Add(level.LevelNumber, level);
            }

            RebuildLevels(remoteLevels: null);
        }

        public void SetRemoteLevels(IReadOnlyList<LevelSummary> remoteLevels)
        {
            RebuildLevels(remoteLevels);
        }

        public bool TryGetLevelSummary(
            int levelNumber,
            out LevelSummary summary)
        {
            return _allLevels.TryGetValue(levelNumber, out summary);
        }

        public bool TryGetFirstLevelSummary(out LevelSummary summary)
        {
            summary = _orderedLevels[0];
            return true;
        }

        public bool TryGetNextLevelSummary(
            int currentLevelNumber,
            out LevelSummary summary)
        {
            if (!_allLevels.ContainsKey(currentLevelNumber))
            {
                summary = default;
                return false;
            }

            for (int i = 0; i < _orderedLevels.Count; i++)
            {
                if (_orderedLevels[i].LevelNumber > currentLevelNumber)
                {
                    summary = _orderedLevels[i];
                    return true;
                }
            }

            summary = default;
            return false;
        }

        private void RebuildLevels(IReadOnlyList<LevelSummary> remoteLevels)
        {
            _allLevels.Clear();

            foreach (KeyValuePair<int, LevelSummary> level in _localLevels)
            {
                _allLevels.Add(level.Key, level.Value);
            }

            if (remoteLevels != null)
            {
                for (int i = 0; i < remoteLevels.Count; i++)
                {
                    LevelSummary level = remoteLevels[i];
                    _allLevels[level.LevelNumber] = level;
                }
            }

            _orderedLevels.Clear();
            _orderedLevels.AddRange(_allLevels.Values);
            _orderedLevels.Sort(
                (left, right) =>
                    left.LevelNumber.CompareTo(right.LevelNumber));
        }
    }
}
