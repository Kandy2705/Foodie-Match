using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Data.Level;
using FoodieMatch.Data.Level.Json;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelCatalogEditorLoader
    {
        public bool TryLoad(out LevelCatalog catalog)
        {
            LevelValidator levelValidator = new(
                new PackageSelectionSettingsValidator(),
                new LevelRandomSettingsValidator(),
                new GrillLayoutValidator());
            ResourcesLevelCatalogLoader loader = new(
                new LevelCatalogJsonParser(),
                new LevelCatalogValidator(levelValidator),
                new LevelCatalogMapper());

            if (loader.TryLoad(out catalog, out LevelValidationResult validationResult))
            {
                return true;
            }

            for (int i = 0; i < validationResult.Errors.Count; i++)
            {
                Debug.LogError(validationResult.Errors[i]);
            }

            return false;
        }
    }
}
