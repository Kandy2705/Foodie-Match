using System;
using FoodieMatch.Core.Domain.Level;
using UnityEditor;

namespace FoodieMatch.Editor.LevelDesign
{
    public static class LevelSeedValidationMenu
    {
        private const int MaximumVisitedStates = 250000;
        private const int MaximumSecondsPerSeed = 10;

        [MenuItem("Foodie Match/Level Design/Validate Package Seeds")]
        public static void ValidatePackageSeeds()
        {
            LevelCatalogEditorLoader catalogLoader = new();

            if (!catalogLoader.TryLoad(out LevelCatalog catalog))
            {
                return;
            }

            LevelSeedSolverSettings settings = new(
                MaximumVisitedStates,
                TimeSpan.FromSeconds(MaximumSecondsPerSeed));
            LevelSeedCatalogValidator validator = new(
                new LevelSeedSolver(settings),
                new LevelSeedValidationReportWriter(),
                new InitialPackageSignatureFactory());

            validator.Validate(catalog);
        }
    }
}
