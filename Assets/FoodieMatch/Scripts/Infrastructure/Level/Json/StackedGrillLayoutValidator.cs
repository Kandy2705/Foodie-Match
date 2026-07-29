using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    internal static class StackedGrillLayoutValidator
    {
        public static void Validate(
            GrillLayoutType? layoutType,
            IReadOnlyList<GrillDto> grills,
            IReadOnlyList<StackedGrillColumnDto> columns,
            IReadOnlyList<GrillMovementGroupDto> movementGroups,
            string levelPath,
            LevelValidationResult result)
        {
            if (!layoutType.HasValue || columns == null)
            {
                return;
            }

            if (layoutType == GrillLayoutType.Standard)
            {
                if (columns.Count > 0)
                {
                    result.AddError(
                        $"{levelPath}.grillColumns must be empty for a standard layout.");
                }

                return;
            }

            if (columns.Count != StackedGrillRules.ColumnCount)
            {
                result.AddError(
                    $"{levelPath}.grillColumns must contain exactly " +
                    $"{StackedGrillRules.ColumnCount} columns.");
            }

            if (movementGroups != null && movementGroups.Count > 0)
            {
                result.AddError(
                    $"{levelPath}.movingGrillGroups must be empty for a stacked-columns layout.");
            }

            Dictionary<int, GrillDto> grillsById = BuildGrillLookup(grills);
            HashSet<int> assignedGrillIds = new();

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                ValidateColumn(
                    columns[columnIndex],
                    columnIndex,
                    grillsById,
                    assignedGrillIds,
                    levelPath,
                    result);
            }

            ValidateAllGrillsAssigned(
                grills,
                assignedGrillIds,
                levelPath,
                result);
            ValidateGrills(grills, levelPath, result);
        }

        private static Dictionary<int, GrillDto> BuildGrillLookup(
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

                if (grill?.Id > 0)
                {
                    grillsById.TryAdd(grill.Id.Value, grill);
                }
            }

            return grillsById;
        }

        private static void ValidateColumn(
            StackedGrillColumnDto column,
            int columnIndex,
            IReadOnlyDictionary<int, GrillDto> grillsById,
            ISet<int> assignedGrillIds,
            string levelPath,
            LevelValidationResult result)
        {
            string columnPath = $"{levelPath}.grillColumns[{columnIndex}]";

            if (column == null)
            {
                result.AddError($"{columnPath} cannot be null.");
                return;
            }

            if (column.GrillIds == null || column.GrillIds.Count == 0)
            {
                result.AddError($"{columnPath}.grillIds must contain at least one grill id.");
                return;
            }

            for (int i = 0; i < column.GrillIds.Count; i++)
            {
                int grillId = column.GrillIds[i];
                string grillIdPath = $"{columnPath}.grillIds[{i}]";

                if (!grillsById.ContainsKey(grillId))
                {
                    result.AddError($"{grillIdPath} references missing grill id {grillId}.");
                    continue;
                }

                if (!assignedGrillIds.Add(grillId))
                {
                    result.AddError(
                        $"{grillIdPath} assigns grill id {grillId} more than once.");
                }
            }
        }

        private static void ValidateAllGrillsAssigned(
            IReadOnlyList<GrillDto> grills,
            ISet<int> assignedGrillIds,
            string levelPath,
            LevelValidationResult result)
        {
            if (grills == null)
            {
                return;
            }

            for (int i = 0; i < grills.Count; i++)
            {
                if (grills[i]?.Id is int grillId &&
                    grillId > 0 &&
                    !assignedGrillIds.Contains(grillId))
                {
                    result.AddError(
                        $"{levelPath}.grills[{i}] with id {grillId} is missing from grillColumns.");
                }
            }
        }

        private static void ValidateGrills(
            IReadOnlyList<GrillDto> grills,
            string levelPath,
            LevelValidationResult result)
        {
            if (grills == null)
            {
                return;
            }

            for (int i = 0; i < grills.Count; i++)
            {
                GrillDto grill = grills[i];

                if (grill == null)
                {
                    continue;
                }

                string grillPath = $"{levelPath}.grills[{i}]";

                if (!string.Equals(
                        grill.Type,
                        "standard",
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError(
                        $"{grillPath}.type must be standard for a stacked-columns layout.");
                }

                if (grill.Trays != null && grill.Trays.Count > 0)
                {
                    result.AddError(
                        $"{grillPath}.trays must be empty for a stacked-columns layout.");
                }
            }
        }
    }
}
