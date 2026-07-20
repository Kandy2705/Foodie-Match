using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class GrillLayoutValidator
    {
        private const int RecommendedMaxFoodDepthSpread = 2;

        public void Validate(
            IReadOnlyList<GrillDto> grills,
            string levelPath,
            LevelValidationResult result)
        {
            if (grills == null || grills.Count == 0)
            {
                result.AddError($"{levelPath}.grills must contain at least one grill.");
                return;
            }

            HashSet<GrillPosition> positions = new();
            Dictionary<int, int> foodCounts = new();
            Dictionary<int, int> minimumFoodDepths = new();
            Dictionary<int, int> maximumFoodDepths = new();

            for (int i = 0; i < grills.Count; i++)
            {
                ValidateGrill(
                    grills[i],
                    i,
                    levelPath,
                    positions,
                    foodCounts,
                    minimumFoodDepths,
                    maximumFoodDepths,
                    result);
            }

            ValidateFoodCounts(foodCounts, levelPath, result);
            AddFoodDepthWarnings(
                minimumFoodDepths,
                maximumFoodDepths,
                levelPath,
                result);
        }

        private static void ValidateGrill(
            GrillDto grill,
            int grillIndex,
            string levelPath,
            ISet<GrillPosition> positions,
            IDictionary<int, int> foodCounts,
            IDictionary<int, int> minimumFoodDepths,
            IDictionary<int, int> maximumFoodDepths,
            LevelValidationResult result)
        {
            string grillPath = $"{levelPath}.grills[{grillIndex}]";

            if (grill == null)
            {
                result.AddError($"{grillPath} cannot be null.");
                return;
            }

            ValidatePosition(grill.Position, grillPath, positions, result);
            ValidateFoodIds(
                grill.FoodIds,
                0,
                $"{grillPath}.foodIds",
                foodCounts,
                minimumFoodDepths,
                maximumFoodDepths,
                result);
            ValidateTrays(
                grill.Trays,
                grillPath,
                foodCounts,
                minimumFoodDepths,
                maximumFoodDepths,
                result);
        }

        private static void ValidatePosition(
            GrillPositionDto position,
            string grillPath,
            ISet<GrillPosition> positions,
            LevelValidationResult result)
        {
            string positionPath = $"{grillPath}.position";

            if (position == null)
            {
                result.AddError($"{positionPath} is required.");
                return;
            }

            bool hasValidX = ValidateCoordinate(position.X, $"{positionPath}.x", result);
            bool hasValidY = ValidateCoordinate(position.Y, $"{positionPath}.y", result);

            if (!hasValidX || !hasValidY)
            {
                return;
            }

            GrillPosition grillPosition = new(position.X.Value, position.Y.Value);

            if (!positions.Add(grillPosition))
            {
                result.AddError($"{positionPath} duplicates another grill position.");
            }
        }

        private static bool ValidateCoordinate(
            float? coordinate,
            string coordinatePath,
            LevelValidationResult result)
        {
            if (!coordinate.HasValue)
            {
                result.AddError($"{coordinatePath} is required.");
                return false;
            }

            if (float.IsNaN(coordinate.Value) || float.IsInfinity(coordinate.Value))
            {
                result.AddError($"{coordinatePath} must be a finite number.");
                return false;
            }

            return true;
        }

        private static void ValidateTrays(
            IReadOnlyList<TrayDto> trays,
            string grillPath,
            IDictionary<int, int> foodCounts,
            IDictionary<int, int> minimumFoodDepths,
            IDictionary<int, int> maximumFoodDepths,
            LevelValidationResult result)
        {
            if (trays == null)
            {
                result.AddError($"{grillPath}.trays is required.");
                return;
            }

            for (int i = 0; i < trays.Count; i++)
            {
                string trayPath = $"{grillPath}.trays[{i}]";
                TrayDto tray = trays[i];

                if (tray == null)
                {
                    result.AddError($"{trayPath} cannot be null.");
                    continue;
                }

                ValidateFoodIds(
                    tray.FoodIds,
                    i + 1,
                    $"{trayPath}.foodIds",
                    foodCounts,
                    minimumFoodDepths,
                    maximumFoodDepths,
                    result);
            }
        }

        private static void ValidateFoodIds(
            IReadOnlyList<int> foodIds,
            int depth,
            string foodIdsPath,
            IDictionary<int, int> foodCounts,
            IDictionary<int, int> minimumFoodDepths,
            IDictionary<int, int> maximumFoodDepths,
            LevelValidationResult result)
        {
            if (foodIds == null)
            {
                result.AddError($"{foodIdsPath} is required.");
                return;
            }

            if (foodIds.Count < BoardRules.MinFoodSlotCount ||
                foodIds.Count > BoardRules.MaxFoodSlotCount)
            {
                result.AddError(
                    $"{foodIdsPath} must contain between " +
                    $"{BoardRules.MinFoodSlotCount} and {BoardRules.MaxFoodSlotCount} slots.");
            }

            bool hasFood = false;

            for (int i = 0; i < foodIds.Count; i++)
            {
                int foodId = foodIds[i];

                if (foodId < BoardRules.EmptyFoodTokenId)
                {
                    result.AddError($"{foodIdsPath}[{i}] cannot be negative.");
                    continue;
                }

                if (foodId == BoardRules.EmptyFoodTokenId)
                {
                    continue;
                }

                hasFood = true;
                AddFoodOccurrence(
                    foodId,
                    depth,
                    foodCounts,
                    minimumFoodDepths,
                    maximumFoodDepths);
            }

            if (!hasFood)
            {
                result.AddError($"{foodIdsPath} must contain at least one food id.");
            }
        }

        private static void AddFoodOccurrence(
            int foodId,
            int depth,
            IDictionary<int, int> foodCounts,
            IDictionary<int, int> minimumFoodDepths,
            IDictionary<int, int> maximumFoodDepths)
        {
            foodCounts.TryGetValue(foodId, out int currentCount);
            foodCounts[foodId] = currentCount + 1;

            if (!minimumFoodDepths.TryGetValue(foodId, out int minimumDepth))
            {
                minimumFoodDepths[foodId] = depth;
                maximumFoodDepths[foodId] = depth;
                return;
            }

            minimumFoodDepths[foodId] = Math.Min(minimumDepth, depth);
            maximumFoodDepths[foodId] = Math.Max(maximumFoodDepths[foodId], depth);
        }

        private static void ValidateFoodCounts(
            IReadOnlyDictionary<int, int> foodCounts,
            string levelPath,
            LevelValidationResult result)
        {
            foreach (KeyValuePair<int, int> foodCount in foodCounts)
            {
                if (foodCount.Value < LevelRules.MinFoodCopies ||
                    foodCount.Value > LevelRules.MaxFoodCopies)
                {
                    result.AddError(
                        $"{levelPath}: food id {foodCount.Key} must appear between " +
                        $"{LevelRules.MinFoodCopies} and {LevelRules.MaxFoodCopies} times.");
                }
            }

            if (foodCounts.Count < LevelRules.ActivePackageCount)
            {
                result.AddError(
                    $"{levelPath} must contain at least " +
                    $"{LevelRules.ActivePackageCount} unique food ids.");
            }
        }

        private static void AddFoodDepthWarnings(
            IReadOnlyDictionary<int, int> minimumFoodDepths,
            IReadOnlyDictionary<int, int> maximumFoodDepths,
            string levelPath,
            LevelValidationResult result)
        {
            foreach (KeyValuePair<int, int> minimumDepth in minimumFoodDepths)
            {
                int maximumDepth = maximumFoodDepths[minimumDepth.Key];

                if (maximumDepth - minimumDepth.Value > RecommendedMaxFoodDepthSpread)
                {
                    result.AddWarning(
                        $"{levelPath}: food id {minimumDepth.Key} spans from depth " +
                        $"{minimumDepth.Value} to {maximumDepth}.");
                }
            }
        }
    }
}
