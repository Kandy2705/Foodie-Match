using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoodieMatch.Core.Application.Level
{
    public enum LevelSynchronizationStage
    {
        CheckingManifest,
        ManifestReady,
        DownloadingPacks,
        Completed
    }

    public readonly struct LevelSynchronizationProgress
    {
        public LevelSynchronizationProgress(
            LevelSynchronizationStage stage,
            int completedPackCount,
            int totalPackCount)
        {
            Stage = stage;
            CompletedPackCount = completedPackCount;
            TotalPackCount = totalPackCount;
        }

        public LevelSynchronizationStage Stage { get; }

        public int CompletedPackCount { get; }

        public int TotalPackCount { get; }
    }

    public interface ILevelSynchronizer
    {
        event Action CatalogUpdated;

        bool IsLevelAvailable(int levelNumber);

        Task<bool> EnsureLevelAvailableAsync(
            int levelNumber,
            Action<LevelSynchronizationProgress> progressChanged,
            CancellationToken cancellationToken);

        Task SynchronizeUpcomingLevelsAsync(
            int currentLevelNumber,
            int followingLevelCount,
            Action<LevelSynchronizationProgress> progressChanged,
            CancellationToken cancellationToken);
    }
}
