using System;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelContentValidator
    {
        private const int SupportedSchemaVersion = 7;

        private readonly LevelValidator _levelValidator;

        public LevelContentValidator(LevelValidator levelValidator)
        {
            _levelValidator = levelValidator ??
                              throw new ArgumentNullException(nameof(levelValidator));
        }

        public void Validate(
            LevelContentDto content,
            LevelCatalogEntryDto catalogEntry,
            int levelIndex,
            LevelValidationResult result)
        {
            string levelPath = $"levels[{levelIndex}]";

            if (content == null)
            {
                result.AddError($"{levelPath} content is required.");
                return;
            }

            ValidateSchemaVersion(content, levelPath, result);
            _levelValidator.Validate(content.Level, levelIndex, result);
            ValidateCatalogMetadata(content.Level, catalogEntry, levelPath, result);
        }

        private static void ValidateSchemaVersion(
            LevelContentDto content,
            string levelPath,
            LevelValidationResult result)
        {
            if (!content.SchemaVersion.HasValue)
            {
                result.AddError($"{levelPath}.schemaVersion is required.");
                return;
            }

            if (content.SchemaVersion.Value != SupportedSchemaVersion)
            {
                result.AddError(
                    $"{levelPath}.schemaVersion {content.SchemaVersion.Value} is not supported. " +
                    $"Expected {SupportedSchemaVersion}.");
            }
        }

        private static void ValidateCatalogMetadata(
            LevelDto level,
            LevelCatalogEntryDto catalogEntry,
            string levelPath,
            LevelValidationResult result)
        {
            if (level == null)
            {
                return;
            }

            if (level.Id.HasValue &&
                catalogEntry.Id.HasValue &&
                level.Id.Value != catalogEntry.Id.Value)
            {
                result.AddError(
                    $"{levelPath}.id {level.Id.Value} does not match catalog id " +
                    $"{catalogEntry.Id.Value}.");
            }

            if (!string.IsNullOrWhiteSpace(level.Difficulty) &&
                !string.IsNullOrWhiteSpace(catalogEntry.Difficulty) &&
                !string.Equals(
                    level.Difficulty,
                    catalogEntry.Difficulty,
                    StringComparison.OrdinalIgnoreCase))
            {
                result.AddError(
                    $"{levelPath}.difficulty '{level.Difficulty}' does not match catalog " +
                    $"difficulty '{catalogEntry.Difficulty}'.");
            }
        }
    }
}
