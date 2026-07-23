namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class PackageSelectionSettingsValidator
    {
        public void Validate(
            PackageSelectionSettingsDto settings,
            string levelPath,
            LevelValidationResult result)
        {
            string settingsPath = $"{levelPath}.packageSelectionWeights";

            if (settings == null)
            {
                result.AddError($"{settingsPath} is required.");
                return;
            }

            ValidateWeights(settings.Early, $"{settingsPath}.early", result);
            ValidateWeights(settings.Middle, $"{settingsPath}.middle", result);
            ValidateWeights(settings.Late, $"{settingsPath}.late", result);
        }

        private static void ValidateWeights(
            PackageSelectionWeightsDto weights,
            string weightsPath,
            LevelValidationResult result)
        {
            if (weights == null)
            {
                result.AddError($"{weightsPath} is required.");
                return;
            }

            ValidateWeight(weights.RackRescue, $"{weightsPath}.rackRescue", result);
            ValidateWeight(weights.ReadyNow, $"{weightsPath}.readyNow", result);
            ValidateWeight(weights.TopTray, $"{weightsPath}.topTray", result);

            if (!weights.RackRescue.HasValue ||
                !weights.ReadyNow.HasValue ||
                !weights.TopTray.HasValue)
            {
                return;
            }

            long totalWeight =
                (long)weights.RackRescue.Value +
                weights.ReadyNow.Value +
                weights.TopTray.Value;

            if (totalWeight <= 0)
            {
                result.AddError($"{weightsPath} must contain at least one positive weight.");
            }
        }

        private static void ValidateWeight(
            int? weight,
            string weightPath,
            LevelValidationResult result)
        {
            if (!weight.HasValue)
            {
                result.AddError($"{weightPath} is required.");
            }
            else if (weight.Value < 0)
            {
                result.AddError($"{weightPath} cannot be negative.");
            }
        }
    }
}
