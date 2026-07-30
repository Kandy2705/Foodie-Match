using System;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelValidator
    {
        private readonly PackageSelectionSettingsValidator _packageSelectionValidator;
        private readonly LevelRandomSettingsValidator _randomSettingsValidator;
        private readonly GrillLayoutValidator _grillLayoutValidator;
        private readonly GrillMovementGroupValidator _grillMovementGroupValidator;

        public LevelValidator(
            PackageSelectionSettingsValidator packageSelectionValidator,
            LevelRandomSettingsValidator randomSettingsValidator,
            GrillLayoutValidator grillLayoutValidator,
            GrillMovementGroupValidator grillMovementGroupValidator)
        {
            _packageSelectionValidator = packageSelectionValidator ??
                                         throw new ArgumentNullException(nameof(packageSelectionValidator));
            _randomSettingsValidator = randomSettingsValidator ??
                                       throw new ArgumentNullException(nameof(randomSettingsValidator));
            _grillLayoutValidator = grillLayoutValidator ??
                                    throw new ArgumentNullException(nameof(grillLayoutValidator));
            _grillMovementGroupValidator = grillMovementGroupValidator ??
                                           throw new ArgumentNullException(
                                               nameof(grillMovementGroupValidator));
        }

        public void Validate(
            LevelDto level,
            string levelPath,
            LevelValidationResult result)
        {
            if (level == null)
            {
                result.AddError($"{levelPath} cannot be null.");
                return;
            }

            ValidateIdentity(level, levelPath, result);
            GrillLayoutType? layoutType =
                ValidateGrillLayoutSchema(level, levelPath, result);
            _randomSettingsValidator.Validate(level.RandomSettings, levelPath, result);
            _packageSelectionValidator.Validate(
                level.PackageSelectionSettings,
                levelPath,
                result);
            _grillLayoutValidator.Validate(level.Grills, levelPath, result);
            StackedGrillLayoutValidator.Validate(
                layoutType,
                level.Grills,
                level.StackedGrillColumns,
                level.MovingGrillGroups,
                levelPath,
                result);
            _grillMovementGroupValidator.Validate(
                level.Grills,
                level.MovingGrillGroups,
                levelPath,
                result);
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

        private static GrillLayoutType? ValidateGrillLayoutSchema(
            LevelDto level,
            string levelPath,
            LevelValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(level.GrillLayoutType) ||
                !Enum.TryParse(level.GrillLayoutType, true, out GrillLayoutType layoutType) ||
                !Enum.IsDefined(typeof(GrillLayoutType), layoutType))
            {
                result.AddError($"{levelPath}.grillLayoutType is invalid.");
                return null;
            }

            if (level.StackedGrillColumns == null)
            {
                result.AddError($"{levelPath}.grillColumns is required.");
            }

            return layoutType;
        }
    }
}
