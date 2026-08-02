using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Level;
using FoodieMatch.Core.Domain.Level;
using FoodieMatch.Infrastructure.Level.Json;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelSynchronizer : ILevelSynchronizer
    {
        private readonly IReadOnlyDictionary<int, string> _bundledContentFiles;
        private readonly LevelCatalogRepository _catalogRepository;
        private readonly RemoteLevelManifestLoader _manifestLoader;
        private readonly RemoteLevelPackCache _packCache;
        private readonly RemoteLevelPackDownloader _packDownloader;
        private readonly Dictionary<long, Task<bool>> _activeDownloads = new();

        public event Action CatalogUpdated;

        public RemoteLevelSynchronizer(
            ResourcesLevelCatalogData bundledCatalog,
            LevelCatalogRepository catalogRepository,
            RemoteLevelManifestLoader manifestLoader,
            RemoteLevelPackCache packCache,
            RemoteLevelPackDownloader packDownloader)
        {
            _bundledContentFiles = bundledCatalog.ContentFiles;
            _catalogRepository = catalogRepository;
            _manifestLoader = manifestLoader;
            _packCache = packCache;
            _packDownloader = packDownloader;
        }

        public bool IsLevelAvailable(int levelNumber)
        {
            if (_bundledContentFiles.ContainsKey(levelNumber))
            {
                return true;
            }

            return _manifestLoader.TryGetManifest(
                       out RemoteLevelManifestDto manifest) &&
                   TryFindPack(
                       manifest,
                       levelNumber,
                       out RemoteLevelPackDto pack) &&
                   _packCache.ContainsLevel(pack, levelNumber);
        }

        public async Task<bool> EnsureLevelAvailableAsync(
            int levelNumber,
            Action<LevelSynchronizationProgress> progressChanged,
            CancellationToken cancellationToken)
        {
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.CheckingManifest);

            if (IsLevelAvailable(levelNumber))
            {
                ReportProgress(
                    progressChanged,
                    LevelSynchronizationStage.Completed);
                return true;
            }

            if (!TryGetDownloadContext(
                    levelNumber,
                    out RemoteLevelPackDto pack,
                    out Uri manifestUri))
            {
                bool refreshed = await _manifestLoader
                    .RefreshForMissingLevelAsync(cancellationToken);

                if (!refreshed ||
                    !TryGetDownloadContext(
                        levelNumber,
                        out pack,
                        out manifestUri))
                {
                    RefreshCatalog();
                    ReportProgress(
                        progressChanged,
                        LevelSynchronizationStage.Completed);
                    return false;
                }
            }

            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.ManifestReady);
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.DownloadingPacks,
                completedPackCount: 0,
                totalPackCount: 1);
            await DownloadPackAsync(
                pack,
                manifestUri,
                cancellationToken);
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.DownloadingPacks,
                completedPackCount: 1,
                totalPackCount: 1);
            RefreshCatalog();
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.Completed);
            return IsLevelAvailable(levelNumber);
        }

        public async Task SynchronizeUpcomingLevelsAsync(
            int currentLevelNumber,
            int followingLevelCount,
            Action<LevelSynchronizationProgress> progressChanged,
            CancellationToken cancellationToken)
        {
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.CheckingManifest);
            await _manifestLoader.RefreshAsync(cancellationToken);
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.ManifestReady);

            if (!_manifestLoader.TryGetManifest(
                    out RemoteLevelManifestDto manifest))
            {
                ReportProgress(
                    progressChanged,
                    LevelSynchronizationStage.Completed);
                return;
            }

            int lastRequestedLevel = checked(
                currentLevelNumber + followingLevelCount);

            if (!ContainsLevel(manifest, currentLevelNumber) ||
                lastRequestedLevel > GetLastLevelNumber(manifest))
            {
                await _manifestLoader.RefreshForMissingLevelAsync(
                    cancellationToken);
                _manifestLoader.TryGetManifest(out manifest);
            }

            RefreshCatalog(manifest);

            if (manifest == null ||
                !_manifestLoader.TryGetManifestUri(out Uri manifestUri))
            {
                ReportProgress(
                    progressChanged,
                    LevelSynchronizationStage.Completed);
                return;
            }

            List<Task<bool>> downloads = new();

            for (int i = 0; i < manifest.Packs.Count; i++)
            {
                RemoteLevelPackDto pack = manifest.Packs[i];

                if (pack.LastLevel.Value < currentLevelNumber ||
                    pack.FirstLevel.Value > lastRequestedLevel)
                {
                    continue;
                }

                downloads.Add(
                    DownloadPackAsync(
                        pack,
                        manifestUri,
                        cancellationToken));
            }

            await WaitForDownloadsAsync(downloads, progressChanged);
            RefreshCatalog(manifest);
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.Completed);
        }

        private static async Task WaitForDownloadsAsync(
            List<Task<bool>> downloads,
            Action<LevelSynchronizationProgress> progressChanged)
        {
            int totalPackCount = downloads.Count;
            int completedPackCount = 0;
            ReportProgress(
                progressChanged,
                LevelSynchronizationStage.DownloadingPacks,
                completedPackCount,
                totalPackCount);

            while (downloads.Count > 0)
            {
                Task<bool> completedDownload = await Task.WhenAny(downloads);
                await completedDownload;
                downloads.Remove(completedDownload);
                completedPackCount++;
                ReportProgress(
                    progressChanged,
                    LevelSynchronizationStage.DownloadingPacks,
                    completedPackCount,
                    totalPackCount);
            }
        }

        private Task<bool> DownloadPackAsync(
            RemoteLevelPackDto pack,
            Uri manifestUri,
            CancellationToken cancellationToken)
        {
            long downloadKey = CreateDownloadKey(pack);

            if (_activeDownloads.TryGetValue(
                    downloadKey,
                    out Task<bool> activeDownload))
            {
                return activeDownload;
            }

            Task<bool> download = DownloadPackAndRemoveAsync(
                downloadKey,
                pack,
                manifestUri,
                cancellationToken);
            _activeDownloads.Add(downloadKey, download);
            return download;
        }

        private async Task<bool> DownloadPackAndRemoveAsync(
            long downloadKey,
            RemoteLevelPackDto pack,
            Uri manifestUri,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                return await _packDownloader.DownloadAsync(
                    pack,
                    manifestUri,
                    cancellationToken);
            }
            finally
            {
                _activeDownloads.Remove(downloadKey);
            }
        }

        private bool TryGetDownloadContext(
            int levelNumber,
            out RemoteLevelPackDto pack,
            out Uri manifestUri)
        {
            pack = null;
            manifestUri = null;

            return _manifestLoader.TryGetManifest(
                       out RemoteLevelManifestDto manifest) &&
                   TryFindPack(manifest, levelNumber, out pack) &&
                   _manifestLoader.TryGetManifestUri(out manifestUri);
        }

        private void RefreshCatalog()
        {
            if (_manifestLoader.TryGetManifest(
                    out RemoteLevelManifestDto manifest))
            {
                RefreshCatalog(manifest);
            }
        }

        private void RefreshCatalog(RemoteLevelManifestDto manifest)
        {
            if (manifest == null)
            {
                return;
            }

            List<LevelSummary> remoteLevels = new();

            for (int i = 0; i < manifest.Packs.Count; i++)
            {
                RemoteLevelPackDto pack = manifest.Packs[i];

                if (_packCache.TryGetLevelSummaries(
                        pack,
                        out IReadOnlyList<LevelSummary> summaries))
                {
                    remoteLevels.AddRange(summaries);
                    continue;
                }

                AddKnownOrPlaceholderLevels(pack, remoteLevels);
            }

            _catalogRepository.SetRemoteLevels(remoteLevels);
            CatalogUpdated?.Invoke();
        }

        private void AddKnownOrPlaceholderLevels(
            RemoteLevelPackDto pack,
            ICollection<LevelSummary> levels)
        {
            for (int levelNumber = pack.FirstLevel.Value;
                 levelNumber <= pack.LastLevel.Value;
                 levelNumber++)
            {
                if (_catalogRepository.TryGetLevelSummary(
                        levelNumber,
                        out LevelSummary knownLevel))
                {
                    levels.Add(knownLevel);
                }
                else
                {
                    levels.Add(
                        new LevelSummary(
                            levelNumber,
                            LevelDifficulty.Normal));
                }
            }
        }

        private static bool TryFindPack(
            RemoteLevelManifestDto manifest,
            int levelNumber,
            out RemoteLevelPackDto pack)
        {
            for (int i = 0; i < manifest.Packs.Count; i++)
            {
                RemoteLevelPackDto candidate = manifest.Packs[i];

                if (levelNumber >= candidate.FirstLevel.Value &&
                    levelNumber <= candidate.LastLevel.Value)
                {
                    pack = candidate;
                    return true;
                }
            }

            pack = null;
            return false;
        }

        private static bool ContainsLevel(
            RemoteLevelManifestDto manifest,
            int levelNumber)
        {
            return TryFindPack(manifest, levelNumber, out _);
        }

        private static int GetLastLevelNumber(
            RemoteLevelManifestDto manifest)
        {
            return manifest.Packs[manifest.Packs.Count - 1]
                .LastLevel.Value;
        }

        private static long CreateDownloadKey(RemoteLevelPackDto pack)
        {
            return ((long)pack.Id.Value << 32) |
                   (uint)pack.Version.Value;
        }

        private static void ReportProgress(
            Action<LevelSynchronizationProgress> progressChanged,
            LevelSynchronizationStage stage,
            int completedPackCount = 0,
            int totalPackCount = 0)
        {
            progressChanged(
                new LevelSynchronizationProgress(
                    stage,
                    completedPackCount,
                    totalPackCount));
        }
    }
}
