using System;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelValidator
    {
        private readonly PackageSelectionSettingsValidator _packageSelectionValidator;
        private readonly LevelRandomSettingsValidator _randomSettingsValidator;
        private readonly GrillLayoutValidator _grillLayoutValidator;

        public LevelValidator(
            PackageSelectionSettingsValidator packageSelectionValidator,
            LevelRandomSettingsValidator randomSettingsValidator,
            GrillLayoutValidator grillLayoutValidator)
        {
            _packageSelectionValidator = packageSelectionValidator ??
                                         throw new ArgumentNullException(nameof(packageSelectionValidator));
            _randomSettingsValidator = randomSettingsValidator ??
                                       throw new ArgumentNullException(nameof(randomSettingsValidator));
            _grillLayoutValidator = grillLayoutValidator ??
                                    throw new ArgumentNullException(nameof(grillLayoutValidator));
        }

        public void Validate(
            LevelDto level,
            int levelIndex,
            LevelValidationResult result)
        {
            string levelPath = $"levels[{levelIndex}]";

            if (level == null)
            {
                result.AddError($"{levelPath} cannot be null.");
                return;
            }

            ValidateIdentity(level, levelPath, result);
            _randomSettingsValidator.Validate(level.RandomSettings, levelPath, result);
            _packageSelectionValidator.Validate(
                level.PackageSelectionSettings,
                levelPath,
                result);
            _grillLayoutValidator.Validate(level.Grills, levelPath, result);
        }

        private static void ValidateIdentity(
            LevelDto level,
            string levelPath,
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
        }
    }
}
