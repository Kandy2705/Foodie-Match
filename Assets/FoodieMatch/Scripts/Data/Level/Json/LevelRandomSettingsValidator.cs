using System.Collections.Generic;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class LevelRandomSettingsValidator
    {
        public void Validate(
            LevelRandomSettingsDto settings,
            string levelPath,
            LevelValidationResult result)
        {
            string settingsPath = $"{levelPath}.randomization";

            if (settings == null)
            {
                result.AddError($"{settingsPath} is required.");
                return;
            }

            ValidatePackageSeeds(settings.PackageSeeds, settingsPath, result);

            if (!settings.GeneratePackageSeedEachRun.HasValue)
            {
                result.AddError(
                    $"{settingsPath}.generatePackageSeedEachRun is required.");
            }

            if (!settings.RandomizeFoodVisualsEachRun.HasValue)
            {
                result.AddError(
                    $"{settingsPath}.randomizeFoodVisualsEachRun is required.");
            }

            if (!settings.FixedFoodVisualSeed.HasValue)
            {
                result.AddError($"{settingsPath}.fixedFoodVisualSeed is required.");
            }
        }

        private static void ValidatePackageSeeds(
            IReadOnlyList<int> packageSeeds,
            string settingsPath,
            LevelValidationResult result)
        {
            string seedsPath = $"{settingsPath}.packageSeeds";

            if (packageSeeds == null || packageSeeds.Count == 0)
            {
                result.AddError($"{seedsPath} must contain at least one seed.");
                return;
            }

            HashSet<int> uniqueSeeds = new();

            for (int i = 0; i < packageSeeds.Count; i++)
            {
                if (!uniqueSeeds.Add(packageSeeds[i]))
                {
                    result.AddError(
                        $"{seedsPath}[{i}] contains duplicated seed {packageSeeds[i]}.");
                }
            }
        }
    }
}
