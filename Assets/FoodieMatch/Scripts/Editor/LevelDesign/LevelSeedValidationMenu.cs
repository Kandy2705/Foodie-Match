using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;
using UnityEditor;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    public static class LevelSeedValidationMenu
    {
        private const int MaximumVisitedStates = 250000;
        private const int MaximumSecondsPerSeed = 10;

        [MenuItem("Foodie Match/Level Design/Validate Package Seeds")]
        public static async void ValidatePackageSeeds()
        {
            try
            {
                LevelCatalogEditorLoader catalogLoader = new();
                IReadOnlyList<LevelDefinition> levels =
                    await catalogLoader.LoadAsync();

                LevelSeedSolverSettings settings = new(
                    MaximumVisitedStates,
                    TimeSpan.FromSeconds(MaximumSecondsPerSeed));
                LevelSeedCatalogValidator validator = new(
                    new LevelSeedSolver(settings),
                    new LevelSeedValidationReportWriter(),
                    new InitialPackageSignatureFactory());

                validator.Validate(levels);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
