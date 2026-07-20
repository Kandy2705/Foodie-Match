using System;
using System.Collections.Generic;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class LevelCatalogValidator
    {
        private const int SupportedSchemaVersion = 2;

        private readonly LevelValidator _levelValidator;

        public LevelCatalogValidator(LevelValidator levelValidator)
        {
            _levelValidator = levelValidator ??
                              throw new ArgumentNullException(nameof(levelValidator));
        }

        public LevelValidationResult Validate(LevelCatalogDto catalog)
        {
            LevelValidationResult result = new();

            if (catalog == null)
            {
                result.AddError("Level catalog is required.");
                return result;
            }

            ValidateSchemaVersion(catalog, result);
            Dictionary<int, LevelDto> levelsById = ValidateLevels(catalog.Levels, result);
            ValidateLevelOrder(catalog.LevelOrder, levelsById, result);
            return result;
        }

        private static void ValidateSchemaVersion(
            LevelCatalogDto catalog,
            LevelValidationResult result)
        {
            if (!catalog.SchemaVersion.HasValue)
            {
                result.AddError("schemaVersion is required.");
                return;
            }

            if (catalog.SchemaVersion.Value != SupportedSchemaVersion)
            {
                result.AddError(
                    $"schemaVersion {catalog.SchemaVersion.Value} is not supported. " +
                    $"Expected {SupportedSchemaVersion}.");
            }
        }

        private Dictionary<int, LevelDto> ValidateLevels(
            IReadOnlyList<LevelDto> levels,
            LevelValidationResult result)
        {
            Dictionary<int, LevelDto> levelsById = new();

            if (levels == null || levels.Count == 0)
            {
                result.AddError("levels must contain at least one level.");
                return levelsById;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDto level = levels[i];
                _levelValidator.Validate(level, i, result);

                if (level?.Id == null || level.Id.Value <= 0)
                {
                    continue;
                }

                if (!levelsById.TryAdd(level.Id.Value, level))
                {
                    result.AddError($"levels[{i}].id {level.Id.Value} is duplicated.");
                }
            }

            return levelsById;
        }

        private static void ValidateLevelOrder(
            IReadOnlyList<int> levelOrder,
            IReadOnlyDictionary<int, LevelDto> levelsById,
            LevelValidationResult result)
        {
            if (levelOrder == null || levelOrder.Count == 0)
            {
                result.AddError("levelOrder must contain at least one level id.");
                return;
            }

            HashSet<int> orderedLevelIds = new();

            for (int i = 0; i < levelOrder.Count; i++)
            {
                int levelId = levelOrder[i];

                if (levelId <= 0)
                {
                    result.AddError($"levelOrder[{i}] must be greater than zero.");
                    continue;
                }

                if (!orderedLevelIds.Add(levelId))
                {
                    result.AddError($"levelOrder[{i}] contains duplicated level id {levelId}.");
                    continue;
                }

                if (!levelsById.ContainsKey(levelId))
                {
                    result.AddError($"levelOrder[{i}] references missing level id {levelId}.");
                }
            }

            foreach (int levelId in levelsById.Keys)
            {
                if (!orderedLevelIds.Contains(levelId))
                {
                    result.AddError($"Level id {levelId} is missing from levelOrder.");
                }
            }
        }
    }
}
