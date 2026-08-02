using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Repositories;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Infrastructure.Level;
using FoodieMatch.Infrastructure.Level.Json;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelCatalogEditorLoader
    {
        public async Task<IReadOnlyList<LevelDefinition>> LoadAsync()
        {
            LevelValidator levelValidator = new(
                new PackageSelectionSettingsValidator(),
                new LevelRandomSettingsValidator(),
                new GrillLayoutValidator(),
                new GrillMovementGroupValidator());
            ResourcesLevelCatalogLoader loader = new(
                new LevelCatalogJsonParser(),
                new LevelCatalogValidator(),
                new LevelCatalogMapper());

            if (!loader.TryLoad(
                    out ResourcesLevelCatalogData catalogData,
                    out LevelValidationResult validationResult))
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        validationResult.Errors));
            }

            ILevelCatalogRepository catalogRepository =
                new LevelCatalogRepository(catalogData.Catalog);
            ILevelRepository levelRepository = new ResourcesLevelRepository(
                catalogRepository,
                catalogData.ContentFiles,
                new LevelContentJsonParser(),
                new LevelContentValidator(levelValidator),
                new LevelContentMapper());
            List<LevelDefinition> levels = new();

            for (int i = 0;
                 i < catalogData.Catalog.OrderedLevels.Count;
                 i++)
            {
                int levelNumber =
                    catalogData.Catalog.OrderedLevels[i].LevelNumber;
                levels.Add(
                    await levelRepository.LoadLevelAsync(levelNumber));
            }

            return levels;
        }
    }
}
