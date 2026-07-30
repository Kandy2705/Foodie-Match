using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelCatalogValidator
    {
        private const int SupportedSchemaVersion = 1;

        public LevelValidationResult Validate(LevelCatalogDto catalog)
        {
            LevelValidationResult result = new();

            if (catalog == null)
            {
                result.AddError("Level catalog is required.");
                return result;
            }

            ValidateSchemaVersion(catalog, result);
            Dictionary<int, LevelCatalogEntryDto> levelsById =
                ValidateLevels(catalog.Levels, result);
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

        private static Dictionary<int, LevelCatalogEntryDto> ValidateLevels(
            IReadOnlyList<LevelCatalogEntryDto> levels,
            LevelValidationResult result)
        {
            Dictionary<int, LevelCatalogEntryDto> levelsById = new();

            if (levels == null || levels.Count == 0)
            {
                result.AddError("levels must contain at least one level.");
                return levelsById;
            }

            HashSet<string> contentFiles = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < levels.Count; i++)
            {
                LevelCatalogEntryDto level = levels[i];
                string levelPath = $"levels[{i}]";

                if (level == null)
                {
                    result.AddError($"{levelPath} cannot be null.");
                    continue;
                }

                ValidateLevelMetadata(level, levelPath, contentFiles, result);

                if (level.Id.HasValue &&
                    level.Id.Value > 0 &&
                    !levelsById.TryAdd(level.Id.Value, level))
                {
                    result.AddError($"levels[{i}].id {level.Id.Value} is duplicated.");
                }
            }

            return levelsById;
        }

        private static void ValidateLevelMetadata(
            LevelCatalogEntryDto level,
            string levelPath,
            ISet<string> contentFiles,
            LevelValidationResult result)
        {
            if (!level.Id.HasValue)
            {
                result.AddError($"{levelPath}.id is required.");
            }
            else if (level.Id.Value <= 0)
            {
                result.AddError($"{levelPath}.id must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(level.Difficulty) ||
                !Enum.TryParse(level.Difficulty, true, out LevelDifficulty difficulty) ||
                !Enum.IsDefined(typeof(LevelDifficulty), difficulty))
            {
                result.AddError($"{levelPath}.difficulty is invalid.");
            }

            if (string.IsNullOrWhiteSpace(level.ContentFile))
            {
                result.AddError($"{levelPath}.contentFile is required.");
            }
            else if (!contentFiles.Add(level.ContentFile))
            {
                result.AddError(
                    $"{levelPath}.contentFile '{level.ContentFile}' is duplicated.");
            }
        }

        private static void ValidateLevelOrder(
            IReadOnlyList<int> levelOrder,
            IReadOnlyDictionary<int, LevelCatalogEntryDto> levelsById,
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
