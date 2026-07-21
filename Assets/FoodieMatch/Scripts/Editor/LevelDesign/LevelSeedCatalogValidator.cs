using System;
using FoodieMatch.Core.Domain.Level;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSeedCatalogValidator
    {
        private readonly LevelSeedSolver _solver;
        private readonly LevelSeedValidationReportWriter _reportWriter;

        public LevelSeedCatalogValidator(
            LevelSeedSolver solver,
            LevelSeedValidationReportWriter reportWriter)
        {
            _solver = solver;
            _reportWriter = reportWriter;
        }

        public void Validate(LevelCatalog catalog)
        {
            LevelSeedValidationReport report = new()
            {
                GeneratedAtUtc = DateTime.UtcNow.ToString("O")
            };
            LevelSeedValidationProgress progress = new(CountSeeds(catalog));
            bool cancelled = false;

            try
            {
                for (int levelIndex = 0;
                     levelIndex < catalog.OrderedLevels.Count && !cancelled;
                     levelIndex++)
                {
                    cancelled = ValidateLevel(
                        catalog.OrderedLevels[levelIndex],
                        report,
                        progress);
                }
            }
            finally
            {
                progress.Clear();
                _reportWriter.Save(report);
            }

            LogSummary(report, cancelled);
        }

        private bool ValidateLevel(
            LevelDefinition level,
            LevelSeedValidationReport report,
            LevelSeedValidationProgress progress)
        {
            for (int seedIndex = 0;
                 seedIndex < level.RandomSettings.PackageSeeds.Count;
                 seedIndex++)
            {
                int packageSeed = level.RandomSettings.PackageSeeds[seedIndex];

                if (progress.Show(level.Id, packageSeed))
                {
                    return true;
                }

                LevelSeedSolverResult result = _solver.Solve(level, packageSeed);
                report.Results.Add(
                    LevelSeedValidationEntry.Create(level.Id, packageSeed, result));
                progress.LogResult(level.Id, packageSeed, result);
            }

            return false;
        }

        private void LogSummary(
            LevelSeedValidationReport report,
            bool cancelled)
        {
            if (cancelled)
            {
                Debug.LogWarning(
                    $"Package seed validation was cancelled. " +
                    $"Report: {_reportWriter.FullReportPath}");
                return;
            }

            bool allSolved = report.Results.TrueForAll(
                result => result.Status == LevelSeedSolverStatus.Solved.ToString());
            string summary = allSolved
                ? "All configured package seeds are solvable."
                : "One or more package seeds could not be validated.";
            Debug.Log($"{summary} Report: {_reportWriter.FullReportPath}");
        }

        private static int CountSeeds(LevelCatalog catalog)
        {
            int count = 0;

            for (int i = 0; i < catalog.OrderedLevels.Count; i++)
            {
                count += catalog.OrderedLevels[i].RandomSettings.PackageSeeds.Count;
            }

            return count;
        }
    }
}
