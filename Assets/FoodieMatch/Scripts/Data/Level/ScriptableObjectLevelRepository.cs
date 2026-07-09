using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Data.Level
{
    public sealed class ScriptableObjectLevelRepository : ILevelRepository
    {
        private readonly List<LevelConfig> _orderedLevels = new();

        public ScriptableObjectLevelRepository(
            LevelCatalogSO levelCatalog,
            LevelDataMapper mapper)
        {
            if (levelCatalog == null)
            {
                throw new ArgumentNullException(nameof(levelCatalog));
            }

            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            LevelValidationResult validationResult = levelCatalog.Validate();

            if (!validationResult.IsValid)
            {
                throw new ArgumentException(
                    "Level catalog is invalid.",
                    nameof(levelCatalog));
            }

            for (int i = 0; i < levelCatalog.Levels.Count; i++)
            {
                LevelDataSO levelData = levelCatalog.Levels[i];

                LevelConfig levelConfig = mapper.Map(levelData);
                _orderedLevels.Add(levelConfig);
            }
        }

        public bool TryGetLevel(
            int levelNumber,
            out LevelConfig levelConfig)
        {
            int levelIndex = levelNumber - 1;

            levelConfig =
                levelIndex >= 0 && levelIndex < _orderedLevels.Count
                    ? _orderedLevels[levelIndex]
                    : null;

            return levelConfig != null;
        }

        public bool TryGetFirstLevel(out LevelConfig levelConfig)
        {
            levelConfig = _orderedLevels.Count > 0
                ? _orderedLevels[0]
                : null;

            return levelConfig != null;
        }

        public bool TryGetNextLevel(
            int currentLevelNumber,
            out LevelConfig levelConfig)
        {
            return TryGetLevel(
                currentLevelNumber + 1,
                out levelConfig);
        }
    }
}
