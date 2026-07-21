using FoodieMatch.Core.Domain.WaitingRack;
using UnityEditor;
using UnityEngine;

namespace FoodieMatch.Editor.LevelDesign
{
    internal sealed class LevelSeedValidationProgress
    {
        private readonly int _totalSeedCount;
        private int _processedSeedCount;

        public LevelSeedValidationProgress(int totalSeedCount)
        {
            _totalSeedCount = totalSeedCount;
        }

        public bool Show(int levelId, int packageSeed)
        {
            float progress = _totalSeedCount > 0
                ? (float)_processedSeedCount / _totalSeedCount
                : 0f;

            return EditorUtility.DisplayCancelableProgressBar(
                "Validating Package Seeds",
                $"Level {levelId}, seed {packageSeed}",
                progress);
        }

        public void LogResult(
            int levelId,
            int packageSeed,
            LevelSeedSolverResult result,
            string initialPackageSignature,
            int? duplicateInitialPackageSeed)
        {
            _processedSeedCount++;
            string message =
                $"Level {levelId}, seed {packageSeed}: {result.Status}. " +
                $"States: {result.VisitedStateCount}, " +
                $"max rack: {result.MaximumRackOccupancy}/{WaitingRackRules.InitialCapacity}, " +
                $"elapsed: {result.Elapsed.TotalMilliseconds:F0} ms, " +
                $"initial packages: {initialPackageSignature}.";

            if (duplicateInitialPackageSeed.HasValue)
            {
                Debug.LogError(
                    $"{message} Initial packages duplicate seed " +
                    $"{duplicateInitialPackageSeed.Value}.");
            }
            else if (result.Status == LevelSeedSolverStatus.Solved)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public void Clear()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
