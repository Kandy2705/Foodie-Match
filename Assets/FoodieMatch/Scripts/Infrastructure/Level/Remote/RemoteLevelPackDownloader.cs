using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelPackDownloader
    {
        private const int DownloadTimeoutSeconds = 4;

        private readonly RemoteLevelPackCache _cache;

        public RemoteLevelPackDownloader(RemoteLevelPackCache cache)
        {
            _cache = cache;
        }

        public async Task<bool> DownloadAsync(
            RemoteLevelPackDto pack,
            Uri rootManifestUri,
            CancellationToken cancellationToken)
        {
            if (_cache.IsAvailable(pack))
            {
                DeleteOtherVersions(pack);
                return true;
            }

            try
            {
                Uri packManifestUri = new(rootManifestUri, pack.ManifestPath);
                byte[] manifestContent = await RemoteFileDownloader.DownloadAsync(
                    packManifestUri,
                    DownloadTimeoutSeconds,
                    cancellationToken);

                if (!_cache.TryParseManifest(
                        manifestContent,
                        pack,
                        out RemoteLevelPackManifestDto manifest))
                {
                    Debug.LogWarning(
                        $"Level pack {pack.Id.Value} manifest is invalid.");
                    return false;
                }

                IReadOnlyDictionary<string, byte[]> levelContents = await DownloadLevelsAsync(
                    packManifestUri,
                    manifest.Levels,
                    cancellationToken);
                bool written = await _cache.WriteAtomicallyAsync(
                    pack,
                    manifestContent,
                    levelContents);

                if (!written)
                {
                    Debug.LogWarning(
                        $"Level pack {pack.Id.Value} contains invalid level data.");
                    return false;
                }

                DeleteOtherVersions(pack);
                return true;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Level pack {pack.Id.Value} could not be downloaded: " +
                    exception.Message);
                return false;
            }
        }

        private static async Task<IReadOnlyDictionary<string, byte[]>> DownloadLevelsAsync(
            Uri packManifestUri,
            IReadOnlyList<RemoteLevelEntryDto> levels,
            CancellationToken cancellationToken)
        {
            Task<byte[]>[] downloadTasks = new Task<byte[]>[levels.Count];

            for (int i = 0; i < levels.Count; i++)
            {
                Uri contentUri = new(packManifestUri, levels[i].ContentPath);
                downloadTasks[i] = RemoteFileDownloader.DownloadAsync(
                    contentUri,
                    DownloadTimeoutSeconds,
                    cancellationToken);
            }

            byte[][] downloadedContent = await Task.WhenAll(downloadTasks);
            Dictionary<string, byte[]> contents = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < levels.Count; i++)
            {
                contents.Add(levels[i].ContentPath, downloadedContent[i]);
            }

            return contents;
        }

        private void DeleteOtherVersions(RemoteLevelPackDto activePack)
        {
            try
            {
                _cache.DeleteOtherVersions(activePack);
            }
            catch (IOException exception)
            {
                Debug.LogWarning(
                    $"Old versions of level pack {activePack.Id.Value} " +
                    $"could not be removed: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning(
                    $"Old versions of level pack {activePack.Id.Value} " +
                    $"could not be removed: {exception.Message}");
            }
        }
    }
}
