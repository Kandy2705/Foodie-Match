using System.Collections.Generic;

namespace FoodieMatch.Infrastructure.Level.Json
{
    internal static class LevelTutorialValidator
    {
        public static void Validate(
            LevelDto level,
            string levelPath,
            LevelValidationResult result)
        {
            if (level.Tutorial == null)
            {
                if (level.Id == 1)
                {
                    result.AddError($"{levelPath}.tutorial is required for level 1.");
                }

                return;
            }

            string sequencePath = $"{levelPath}.tutorial.foodSelectionSequence";
            IReadOnlyList<FoodSelectionTutorialStepDto> sequence =
                level.Tutorial.FoodSelectionSequence;

            if (sequence == null || sequence.Count == 0)
            {
                result.AddError($"{sequencePath} must contain at least one step.");
                return;
            }

            Dictionary<int, GrillDto> grillsById = CreateGrillLookup(level.Grills);
            HashSet<(int GrillId, int FoodSlotIndex)> selectedFoods = new();

            for (int i = 0; i < sequence.Count; i++)
            {
                ValidateStep(
                    sequence[i],
                    i,
                    sequencePath,
                    grillsById,
                    selectedFoods,
                    result);
            }
        }

        private static Dictionary<int, GrillDto> CreateGrillLookup(
            IReadOnlyList<GrillDto> grills)
        {
            Dictionary<int, GrillDto> grillsById = new();

            if (grills == null)
            {
                return grillsById;
            }

            for (int i = 0; i < grills.Count; i++)
            {
                GrillDto grill = grills[i];

                if (grill?.Id.HasValue == true && !grillsById.ContainsKey(grill.Id.Value))
                {
                    grillsById.Add(grill.Id.Value, grill);
                }
            }

            return grillsById;
        }

        private static void ValidateStep(
            FoodSelectionTutorialStepDto step,
            int stepIndex,
            string sequencePath,
            IReadOnlyDictionary<int, GrillDto> grillsById,
            ISet<(int GrillId, int FoodSlotIndex)> selectedFoods,
            LevelValidationResult result)
        {
            string stepPath = $"{sequencePath}[{stepIndex}]";

            if (step == null)
            {
                result.AddError($"{stepPath} cannot be null.");
                return;
            }

            if (!step.GrillId.HasValue || !grillsById.TryGetValue(step.GrillId.Value, out GrillDto grill))
            {
                result.AddError($"{stepPath}.grillId must reference an existing grill.");
                return;
            }

            if (!step.FoodSlotIndex.HasValue ||
                grill.FoodIds == null ||
                step.FoodSlotIndex.Value < 0 ||
                step.FoodSlotIndex.Value >= grill.FoodIds.Count)
            {
                result.AddError($"{stepPath}.foodSlotIndex must reference an active grill food slot.");
                return;
            }

            (int GrillId, int FoodSlotIndex) foodAddress =
                (step.GrillId.Value, step.FoodSlotIndex.Value);

            if (!selectedFoods.Add(foodAddress))
            {
                result.AddError($"{stepPath} duplicates another tutorial food address.");
            }

            int foodId = grill.FoodIds[step.FoodSlotIndex.Value];

            if (foodId <= 0)
            {
                result.AddError($"{stepPath} cannot reference an empty food slot.");
            }
        }
    }
}
