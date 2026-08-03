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
        private readonly RemoteLevelPackArchiveReader _archiveReader;

        public RemoteLevelPackDownloader(
            RemoteLevelPackCache cache,
            RemoteLevelPackArchiveReader archiveReader)
        {
            _cache = cache;
            _archiveReader = archiveReader;
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
                Uri archiveUri = new(rootManifestUri, pack.ArchivePath);
                byte[] archiveContent = await RemoteFileDownloader.DownloadAsync(
                    archiveUri,
                    DownloadTimeoutSeconds,
                    cancellationToken);

                if (!RemoteLevelFileHash.Matches(
                        archiveContent,
                        pack.ArchiveSha256))
                {
                    Debug.LogWarning(
                        $"Level pack {pack.Id.Value} archive hash is invalid.");
                    return false;
                }

                if (!_archiveReader.TryRead(
                        archiveContent,
                        out byte[] manifestContent,
                        out IReadOnlyDictionary<string, byte[]>
                            levelContents))
                {
                    Debug.LogWarning(
                        $"Level pack {pack.Id.Value} archive is invalid.");
                    return false;
                }

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
