using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Data.Level.Json
{
    public sealed class LevelCatalogMapper
    {
        public LevelCatalog Map(LevelCatalogDto catalogDto)
        {
            if (catalogDto == null)
            {
                throw new ArgumentNullException(nameof(catalogDto));
            }

            Dictionary<int, LevelDto> levelsById = new();

            for (int i = 0; i < catalogDto.Levels.Count; i++)
            {
                LevelDto levelDto = catalogDto.Levels[i];
                levelsById.Add(levelDto.Id.Value, levelDto);
            }

            List<LevelDefinition> orderedLevels = new();

            for (int i = 0; i < catalogDto.LevelOrder.Count; i++)
            {
                int levelId = catalogDto.LevelOrder[i];
                orderedLevels.Add(MapLevel(levelsById[levelId]));
            }

            return new LevelCatalog(orderedLevels);
        }

        private static LevelDefinition MapLevel(LevelDto levelDto)
        {
            Enum.TryParse(
                levelDto.Difficulty,
                ignoreCase: true,
                out LevelDifficulty difficulty);

            return new LevelDefinition(
                levelDto.Id.Value,
                difficulty,
                MapRandomSettings(levelDto.RandomSettings),
                MapPackageSelectionSettings(levelDto.PackageSelectionSettings),
                MapGrills(levelDto.Grills));
        }

        private static LevelRandomSettings MapRandomSettings(
            LevelRandomSettingsDto settingsDto)
        {
            return new LevelRandomSettings(
                settingsDto.PackageSeeds,
                settingsDto.GeneratePackageSeedEachRun.Value,
                settingsDto.RandomizeFoodVisualsEachRun.Value,
                settingsDto.FixedFoodVisualSeed.Value);
        }

        private static PackageSelectionSettings MapPackageSelectionSettings(
            PackageSelectionSettingsDto settingsDto)
        {
            return new PackageSelectionSettings(
                MapWeights(settingsDto.Early),
                MapWeights(settingsDto.Middle),
                MapWeights(settingsDto.Late));
        }

        private static PackageSelectionWeights MapWeights(
            PackageSelectionWeightsDto weightsDto)
        {
            return new PackageSelectionWeights(
                weightsDto.RackRescue.Value,
                weightsDto.ReadyNow.Value,
                weightsDto.TopTray.Value);
        }

        private static IReadOnlyList<GrillDefinition> MapGrills(
            IReadOnlyList<GrillDto> grillDtos)
        {
            List<GrillDefinition> grills = new();

            for (int i = 0; i < grillDtos.Count; i++)
            {
                GrillDto grillDto = grillDtos[i];

                grills.Add(
                    new GrillDefinition(
                        new GrillPosition(
                            grillDto.Position.X.Value,
                            grillDto.Position.Y.Value),
                        grillDto.FoodIds,
                        MapTrays(grillDto.Trays)));
            }

            return grills;
        }

        private static IReadOnlyList<TrayDefinition> MapTrays(
            IReadOnlyList<TrayDto> trayDtos)
        {
            List<TrayDefinition> trays = new();

            for (int i = 0; i < trayDtos.Count; i++)
            {
                trays.Add(new TrayDefinition(trayDtos[i].FoodIds));
            }

            return trays;
        }
    }
}
