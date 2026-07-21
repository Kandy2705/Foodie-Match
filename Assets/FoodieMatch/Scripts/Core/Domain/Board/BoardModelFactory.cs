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
                        i,
                        grill.Position,
                        grill.FoodTokenIds,
                        CreateTrays(grill.Trays)));
            }

            return new BoardModel(grills);
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
