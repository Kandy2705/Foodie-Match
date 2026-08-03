using System;
using FoodieMatch.Core.Domain.Level;

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
            LevelSummary expectedSummary,
            LevelValidationResult result)
        {
            string levelPath = $"level {expectedSummary.LevelNumber}";

            if (content == null)
            {
                result.AddError($"{levelPath} content is required.");
                return;
            }

            ValidateSchemaVersion(content, levelPath, result);
            _levelValidator.Validate(content.Level, levelPath, result);
            ValidateCatalogMetadata(
                content.Level,
                expectedSummary,
                levelPath,
                result);
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
            LevelSummary expectedSummary,
            string levelPath,
            LevelValidationResult result)
        {
            if (level == null)
            {
                return;
            }

            if (level.Id.HasValue &&
                level.Id.Value != expectedSummary.LevelNumber)
            {
                result.AddError(
                    $"{levelPath}.id {level.Id.Value} does not match catalog id " +
                    $"{expectedSummary.LevelNumber}.");
            }

            if (!string.IsNullOrWhiteSpace(level.Difficulty) &&
                Enum.TryParse(
                    level.Difficulty,
                    ignoreCase: true,
                    out LevelDifficulty difficulty) &&
                difficulty != expectedSummary.Difficulty)
            {
                result.AddError(
                    $"{levelPath}.difficulty '{level.Difficulty}' does not match catalog " +
                    $"difficulty '{expectedSummary.Difficulty}'.");
            }
        }
    }
}
