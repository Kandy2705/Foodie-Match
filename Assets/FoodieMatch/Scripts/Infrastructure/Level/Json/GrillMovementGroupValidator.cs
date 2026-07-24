using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class GrillMovementGroupValidator
    {
        private const float PositionTolerance = 0.001f;

        public void Validate(
            IReadOnlyList<GrillDto> grills,
            IReadOnlyList<GrillMovementGroupDto> movementGroups,
            string levelPath,
            LevelValidationResult result)
        {
            Dictionary<int, GrillDto> grillsById =
                ValidateGrillIds(grills, levelPath, result);

            if (movementGroups == null)
            {
                result.AddError($"{levelPath}.movingGrillGroups is required.");
                return;
            }

            HashSet<int> movingGrillIds = new();

            for (int i = 0; i < movementGroups.Count; i++)
            {
                ValidateMovementGroup(
                    movementGroups[i],
                    i,
                    levelPath,
                    grillsById,
                    movingGrillIds,
                    result);
            }
        }

        private static Dictionary<int, GrillDto> ValidateGrillIds(
            IReadOnlyList<GrillDto> grills,
            string levelPath,
            LevelValidationResult result)
        {
            Dictionary<int, GrillDto> grillsById = new();

            if (grills == null)
            {
                return grillsById;
            }

            for (int i = 0; i < grills.Count; i++)
            {
                GrillDto grill = grills[i];

                if (grill == null)
                {
                    continue;
                }

                string idPath = $"{levelPath}.grills[{i}].id";

                if (!grill.Id.HasValue)
                {
                    result.AddError($"{idPath} is required.");
                    continue;
                }

                if (grill.Id.Value <= 0)
                {
                    result.AddError($"{idPath} must be greater than zero.");
                    continue;
                }

                if (!grillsById.TryAdd(grill.Id.Value, grill))
                {
                    result.AddError(
                        $"{idPath} duplicates grill id {grill.Id.Value}.");
                }
            }

            return grillsById;
        }

        private static void ValidateMovementGroup(
            GrillMovementGroupDto movementGroup,
            int movementGroupIndex,
            string levelPath,
            IReadOnlyDictionary<int, GrillDto> grillsById,
            ISet<int> movingGrillIds,
            LevelValidationResult result)
        {
            string groupPath =
                $"{levelPath}.movingGrillGroups[{movementGroupIndex}]";

            if (movementGroup == null)
            {
                result.AddError($"{groupPath} cannot be null.");
                return;
            }

            bool hasValidDirection = TryValidateDirection(
                movementGroup.Direction,
                groupPath,
                result,
                out GrillMovementDirection direction);
            ValidateMovementSpeed(movementGroup.MovementSpeed, groupPath, result);

            if (movementGroup.GrillIds == null ||
                movementGroup.GrillIds.Count < 2)
            {
                result.AddError(
                    $"{groupPath}.grillIds must contain at least two grill ids.");
                return;
            }

            HashSet<int> groupGrillIds = new();
            List<GrillDto> groupGrills = new(movementGroup.GrillIds.Count);

            for (int i = 0; i < movementGroup.GrillIds.Count; i++)
            {
                int grillId = movementGroup.GrillIds[i];
                string grillIdPath = $"{groupPath}.grillIds[{i}]";

                if (grillId <= 0)
                {
                    result.AddError($"{grillIdPath} must be greater than zero.");
                    continue;
                }

                if (!groupGrillIds.Add(grillId))
                {
                    result.AddError(
                        $"{grillIdPath} duplicates grill id {grillId} in the same group.");
                    continue;
                }

                if (!movingGrillIds.Add(grillId))
                {
                    result.AddError(
                        $"{grillIdPath} references grill id {grillId}, " +
                        "which already belongs to another movement group.");
                }

                if (!grillsById.TryGetValue(grillId, out GrillDto grill))
                {
                    result.AddError(
                        $"{grillIdPath} references missing grill id {grillId}.");
                    continue;
                }

                groupGrills.Add(grill);
            }

            if (hasValidDirection &&
                groupGrills.Count == movementGroup.GrillIds.Count)
            {
                ValidateGroupLayout(
                    groupGrills,
                    direction,
                    groupPath,
                    result);
            }
        }

        private static bool TryValidateDirection(
            string value,
            string groupPath,
            LevelValidationResult result,
            out GrillMovementDirection direction)
        {
            direction = default;
            bool isValid =
                !string.IsNullOrWhiteSpace(value) &&
                Enum.TryParse(value, true, out direction) &&
                Enum.IsDefined(typeof(GrillMovementDirection), direction);

            if (!isValid)
            {
                result.AddError($"{groupPath}.direction is invalid.");
            }

            return isValid;
        }

        private static void ValidateMovementSpeed(
            float? movementSpeed,
            string groupPath,
            LevelValidationResult result)
        {
            if (!movementSpeed.HasValue)
            {
                result.AddError($"{groupPath}.movementSpeed is required.");
                return;
            }

            if (movementSpeed.Value <= 0f ||
                float.IsNaN(movementSpeed.Value) ||
                float.IsInfinity(movementSpeed.Value))
            {
                result.AddError(
                    $"{groupPath}.movementSpeed must be a finite number greater than zero.");
            }
        }

        private static void ValidateGroupLayout(
            IReadOnlyList<GrillDto> grills,
            GrillMovementDirection direction,
            string groupPath,
            LevelValidationResult result)
        {
            bool isHorizontal =
                direction == GrillMovementDirection.Left ||
                direction == GrillMovementDirection.Right;
            List<float> movementPositions = new(grills.Count);
            float fixedPosition = 0f;

            for (int i = 0; i < grills.Count; i++)
            {
                GrillPositionDto position = grills[i].Position;

                if (position?.X == null || position.Y == null)
                {
                    return;
                }

                float currentFixedPosition =
                    isHorizontal ? position.Y.Value : position.X.Value;
                float movementPosition =
                    isHorizontal ? position.X.Value : position.Y.Value;

                if (i == 0)
                {
                    fixedPosition = currentFixedPosition;
                }
                else if (!Approximately(fixedPosition, currentFixedPosition))
                {
                    result.AddError(
                        $"{groupPath}.grillIds must reference grills on the same " +
                        $"{(isHorizontal ? "horizontal" : "vertical")} line.");
                    return;
                }

                movementPositions.Add(movementPosition);
            }

            movementPositions.Sort();
            float expectedSpacing =
                movementPositions[1] -
                movementPositions[0];

            if (expectedSpacing <= PositionTolerance)
            {
                result.AddError(
                    $"{groupPath}.grillIds must reference distinct grill positions.");
                return;
            }

            for (int i = 2; i < movementPositions.Count; i++)
            {
                float spacing =
                    movementPositions[i] -
                    movementPositions[i - 1];

                if (!Approximately(expectedSpacing, spacing))
                {
                    result.AddError(
                        $"{groupPath}.grillIds must reference evenly spaced grills.");
                    return;
                }
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= PositionTolerance;
        }
    }
}
