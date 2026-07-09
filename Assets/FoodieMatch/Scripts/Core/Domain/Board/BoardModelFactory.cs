using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Domain.Board
{
    public sealed class BoardModelFactory
    {
        public BoardModel Create(LevelConfig levelConfig)
        {
            if (levelConfig == null)
            {
                throw new ArgumentNullException(nameof(levelConfig));
            }

            List<GrillModel> grills = new List<GrillModel>();

            for (int i = 0; i < levelConfig.Grills.Count; i++)
            {
                GrillConfig grillConfig = levelConfig.Grills[i];

                grills.Add(
                    new GrillModel(
                        grillConfig.PositionIndex,
                        grillConfig.InitialFoodTokenIds,
                        CreateTrays(grillConfig.Trays)));
            }

            return new BoardModel(grills);
        }

        private static IReadOnlyList<TrayModel> CreateTrays(
            IReadOnlyList<TrayConfig> trayConfigs)
        {
            List<TrayModel> trays = new List<TrayModel>();

            for (int i = 0; i < trayConfigs.Count; i++)
            {
                trays.Add(new TrayModel(trayConfigs[i].FoodTokenIds));
            }

            return trays;
        }
    }
}
