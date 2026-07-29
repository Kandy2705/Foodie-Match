using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Domain.Board
{
    public sealed class BoardModelFactory
    {
        public BoardModel Create(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            List<GrillModel> grills = new();

            for (int i = 0; i < level.Grills.Count; i++)
            {
                GrillDefinition grill = level.Grills[i];

                grills.Add(
                    new GrillModel(
                        grill.Id,
                        i,
                        grill.Position,
                        grill.Type,
                        grill.FoodTokenIds,
                        CreateTrays(grill.Trays)));
            }

            return new BoardModel(
                grills,
                level.GrillLayoutType,
                CreateStackedGrillColumns(level, grills));
        }

        private static IReadOnlyList<StackedGrillColumnState> CreateStackedGrillColumns(
            LevelDefinition level,
            IReadOnlyList<GrillModel> grills)
        {
            Dictionary<int, int> positionIndexByGrillId = new();

            for (int i = 0; i < grills.Count; i++)
            {
                positionIndexByGrillId.Add(grills[i].Id, grills[i].PositionIndex);
            }

            List<StackedGrillColumnState> columns = new();

            for (int columnIndex = 0;
                 columnIndex < level.StackedGrillColumns.Count;
                 columnIndex++)
            {
                IReadOnlyList<int> grillIds =
                    level.StackedGrillColumns[columnIndex].GrillIds;
                List<int> grillPositionIndices = new(grillIds.Count);

                for (int grillIndex = 0; grillIndex < grillIds.Count; grillIndex++)
                {
                    grillPositionIndices.Add(positionIndexByGrillId[grillIds[grillIndex]]);
                }

                columns.Add(new StackedGrillColumnState(grillPositionIndices));
            }

            return columns;
        }

        private static IReadOnlyList<TrayModel> CreateTrays(
            IReadOnlyList<TrayDefinition> trayDefinitions)
        {
            List<TrayModel> trays = new();

            for (int i = 0; i < trayDefinitions.Count; i++)
            {
                trays.Add(new TrayModel(trayDefinitions[i].FoodTokenIds));
            }

            return trays;
        }
    }
}
