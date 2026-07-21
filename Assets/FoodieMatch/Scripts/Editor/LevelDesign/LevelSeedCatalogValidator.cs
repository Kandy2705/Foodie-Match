using System;
using System.Collections.Generic;
using System.Text;
using FoodieMatch.Core.Application.Randomization;
using FoodieMatch.Core.Application.UseCases;
using FoodieMatch.Core.Domain.Board;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Core.Domain.RequiredPackage;
using FoodieMatch.Core.Domain.WaitingRack;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSeedCatalogValidator
    {
        private readonly LevelSeedSolver _solver;
        private readonly LevelSeedValidationReportWriter _reportWriter;
        private readonly InitialPackageSignatureFactory _initialPackageSignatureFactory;

        public LevelSeedCatalogValidator(
            LevelSeedSolver solver,
            LevelSeedValidationReportWriter reportWriter,
            InitialPackageSignatureFactory initialPackageSignatureFactory)
        {
            _solver = solver;
            _reportWriter = reportWriter;
            _initialPackageSignatureFactory = initialPackageSignatureFactory;
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
            Dictionary<string, int> seedByInitialPackageSignature = new();

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
                string initialPackageSignature = CreateInitialPackageSignature(
                    level,
                    packageSeed);
                int? duplicateInitialPackageSeed = FindDuplicateInitialPackageSeed(
                    packageSeed,
                    initialPackageSignature,
                    seedByInitialPackageSignature);
                report.Results.Add(
                    LevelSeedValidationEntry.Create(
                        level.Id,
                        packageSeed,
                        result,
                        initialPackageSignature,
                        duplicateInitialPackageSeed));
                progress.LogResult(
                    level.Id,
                    packageSeed,
                    result,
                    initialPackageSignature,
                    duplicateInitialPackageSeed);
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

            bool allValid = report.Results.TrueForAll(
                result =>
                    result.Status == LevelSeedSolverStatus.Solved.ToString() &&
                    !result.DuplicateInitialPackageSeed.HasValue);
            string summary = allValid
                ? "All configured package seeds are solvable and have unique initial packages."
                : "One or more package seeds failed solvability or initial package diversity validation.";
            Debug.Log($"{summary} Report: {_reportWriter.FullReportPath}");
        }

        private string CreateInitialPackageSignature(
            LevelDefinition level,
            int packageSeed)
        {
            return _initialPackageSignatureFactory.TryCreate(
                level,
                packageSeed,
                out string signature)
                ? signature
                : null;
        }

        private static int? FindDuplicateInitialPackageSeed(
            int packageSeed,
            string initialPackageSignature,
            IDictionary<string, int> seedByInitialPackageSignature)
        {
            if (string.IsNullOrEmpty(initialPackageSignature))
            {
                return null;
            }

            if (seedByInitialPackageSignature.TryGetValue(
                    initialPackageSignature,
                    out int existingSeed))
            {
                return existingSeed;
            }

            seedByInitialPackageSignature.Add(initialPackageSignature, packageSeed);
            return null;
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

    internal sealed class InitialPackageSignatureFactory
    {
        private readonly BoardModelFactory _boardModelFactory = new();
        private readonly RequiredPackageLifecycleUseCase _packageLifecycleUseCase = new(
            new RequiredPackageGenerator(),
            new RequiredPackageMatcher());

        public bool TryCreate(
            LevelDefinition level,
            int packageSeed,
            out string signature)
        {
            BoardModel board = _boardModelFactory.Create(level);
            WaitingRackModel waitingRack = new(WaitingRackRules.InitialCapacity);
            PackageRandom packageRandom = new(packageSeed);

            if (!_packageLifecycleUseCase.TryCreateInitialPackages(
                    board,
                    waitingRack,
                    level.PackageSelectionSettings,
                    packageRandom,
                    out RequiredPackageModel[] packages))
            {
                signature = null;
                return false;
            }

            StringBuilder signatureBuilder = new();

            for (int packageIndex = 0; packageIndex < packages.Length; packageIndex++)
            {
                if (packageIndex > 0)
                {
                    signatureBuilder.Append(" > ");
                }

                RequiredPackageModel package = packages[packageIndex];
                signatureBuilder.Append(package.FoodTokenId);
                signatureBuilder.Append('x');
                signatureBuilder.Append(package.RequiredAmount);
            }

            signature = signatureBuilder.ToString();
            return true;
        }
    }
}
