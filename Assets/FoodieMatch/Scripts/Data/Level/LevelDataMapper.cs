using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Data.Level
{
    public sealed class LevelDataMapper
    {
        public LevelConfig Map(LevelDataSO levelData)
        {
            if (levelData == null)
            {
                throw new ArgumentNullException(nameof(levelData));
            }

            List<GrillConfig> grills = new List<GrillConfig>();

            for (int i = 0; i < levelData.Grills.Count; i++)
            {
                GrillData grillData = levelData.Grills[i];

                grills.Add(
                    new GrillConfig(
                        grillData.PositionIndex,
                        grillData.InitialFoodTokenIds,
                        MapTrays(grillData.Trays)));
            }

            return new LevelConfig(
                levelData.WaitingRackCapacity,
                levelData.MaxPackageSlotCount,
                grills);
        }

        private static IReadOnlyList<TrayConfig> MapTrays(
            IReadOnlyList<TrayData> trayDataList)
        {
            List<TrayConfig> trays = new List<TrayConfig>();

            if (trayDataList == null)
            {
                return trays;
            }

            for (int i = 0; i < trayDataList.Count; i++)
            {
                trays.Add(new TrayConfig(trayDataList[i].FoodTokenIds));
            }

            return trays;
        }
    }
}
