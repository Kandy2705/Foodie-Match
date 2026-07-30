using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Grill;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Infrastructure.Level.Json
{
    public sealed class LevelCatalogMapper
    {
        public LevelCatalog Map(
            LevelCatalogDto catalogDto,
            IReadOnlyDictionary<int, LevelDto> levelsById)
        {
            if (catalogDto == null)
            {
                throw new ArgumentNullException(nameof(catalogDto));
            }

            if (levelsById == null)
            {
                throw new ArgumentNullException(nameof(levelsById));
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
                MapGrillLayoutType(levelDto.GrillLayoutType),
                MapRandomSettings(levelDto.RandomSettings),
                MapPackageSelectionSettings(levelDto.PackageSelectionSettings),
                MapMovementGroups(levelDto.MovingGrillGroups),
                MapStackedGrillColumns(levelDto.StackedGrillColumns),
                MapGrills(levelDto.Grills));
        }

        private static GrillLayoutType MapGrillLayoutType(string type)
        {
            if (string.Equals(type, "standard", StringComparison.OrdinalIgnoreCase))
            {
                return GrillLayoutType.Standard;
            }

            if (string.Equals(type, "stackedColumns", StringComparison.OrdinalIgnoreCase))
            {
                return GrillLayoutType.StackedColumns;
            }

            throw new ArgumentException($"Unsupported grill layout type: {type}.", nameof(type));
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
                        grillDto.Id.Value,
                        MapGrillType(grillDto.Type),
                        new GrillPosition(
                            grillDto.Position.X.Value,
                            grillDto.Position.Y.Value),
                        grillDto.FoodIds,
                        MapTrays(grillDto.Trays)));
            }

            return grills;
        }

        private static IReadOnlyList<StackedGrillColumnDefinition> MapStackedGrillColumns(
            IReadOnlyList<StackedGrillColumnDto> columnDtos)
        {
            List<StackedGrillColumnDefinition> columns = new();

            for (int i = 0; i < columnDtos.Count; i++)
            {
                columns.Add(new StackedGrillColumnDefinition(columnDtos[i].GrillIds));
            }

            return columns;
        }

        private static GrillType MapGrillType(string type)
        {
            if (string.Equals(type, "standard", StringComparison.OrdinalIgnoreCase))
            {
                return GrillType.Standard;
            }

            if (string.Equals(type, "single", StringComparison.OrdinalIgnoreCase))
            {
                return GrillType.Single;
            }

            throw new ArgumentException($"Unsupported grill type: {type}.", nameof(type));
        }

        private static IReadOnlyList<GrillMovementGroupDefinition>
            MapMovementGroups(
                IReadOnlyList<GrillMovementGroupDto> movementGroupDtos)
        {
            List<GrillMovementGroupDefinition> movementGroups = new();

            for (int i = 0; i < movementGroupDtos.Count; i++)
            {
                GrillMovementGroupDto movementGroupDto = movementGroupDtos[i];

                Enum.TryParse(
                    movementGroupDto.Direction,
                    ignoreCase: true,
                    out GrillMovementDirection direction);

                movementGroups.Add(
                    new GrillMovementGroupDefinition(
                        direction,
                        movementGroupDto.GrillIds,
                        movementGroupDto.MovementSpeed.Value));
            }

            return movementGroups;
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
