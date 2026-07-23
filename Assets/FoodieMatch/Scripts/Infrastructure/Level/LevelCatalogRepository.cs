using System;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level
{
    public sealed class LevelCatalogRepository : ILevelRepository
    {
        private readonly LevelCatalog _catalog;

        public LevelCatalogRepository(LevelCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public bool TryGetLevel(int levelNumber, out LevelDefinition level)
        {
            int levelIndex = levelNumber - 1;
            level = levelIndex >= 0 && levelIndex < _catalog.OrderedLevels.Count
                ? _catalog.OrderedLevels[levelIndex]
                : null;

            return level != null;
        }

        public bool TryGetFirstLevel(out LevelDefinition level)
        {
            return TryGetLevel(levelNumber: 1, out level);
        }

        public bool TryGetNextLevel(int currentLevelNumber, out LevelDefinition level)
        {
            return TryGetLevel(currentLevelNumber + 1, out level);
        }
    }
}
