using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase.RemoteConfig;
using FoodieMatch.Infrastructure.RemoteConfig;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelManifestLoader
    {
        private const int DownloadTimeoutSeconds = 4;

        private readonly RemoteLevelManifestCache _cache;
        private readonly string _fallbackManifestUrl;

        private Uri _manifestUri;

        public RemoteLevelManifestLoader(
            RemoteLevelManifestCache cache,
            string fallbackManifestUrl)
        {
            _cache = cache;
            _fallbackManifestUrl = fallbackManifestUrl;
        }

        public async Task<bool> RefreshAsync(
            CancellationToken cancellationToken)
        {
            if (!TryResolveManifestSettings(
                    out Uri manifestUri,
                    out int? expectedManifestVersion))
            {
                return _cache.TryLoad(out _);
            }

            _manifestUri = manifestUri;

            if (_cache.TryLoad(out RemoteLevelManifestDto cachedManifest) &&
                (!expectedManifestVersion.HasValue ||
                 cachedManifest.ManifestVersion ==
                 expectedManifestVersion.Value))
            {
                return true;
            }

            return await DownloadAndCacheAsync(
                manifestUri,
                expectedManifestVersion,
                cancellationToken);
        }

        public async Task<bool> RefreshForMissingLevelAsync(
            CancellationToken cancellationToken)
        {
            if (!TryResolveManifestSettings(
                    out Uri manifestUri,
                    out int? expectedManifestVersion))
            {
                return false;
            }

            _manifestUri = manifestUri;
            return await DownloadAndCacheAsync(
                manifestUri,
                expectedManifestVersion,
                cancellationToken);
        }

        public bool TryGetManifest(out RemoteLevelManifestDto manifest)
        {
            return _cache.TryLoad(out manifest);
        }

        public bool TryGetManifestUri(out Uri manifestUri)
        {
            if (_manifestUri != null)
            {
                manifestUri = _manifestUri;
                return true;
            }

            if (TryResolveManifestSettings(out manifestUri, out _))
            {
                _manifestUri = manifestUri;
                return true;
            }

            return false;
        }

        private async Task<bool> DownloadAndCacheAsync(
            Uri manifestUri,
            int? expectedManifestVersion,
            CancellationToken cancellationToken)
        {
            try
            {
                byte[] content = await RemoteFileDownloader.DownloadAsync(
                    manifestUri,
                    DownloadTimeoutSeconds,
                    cancellationToken);
                bool written = await _cache.WriteAtomicallyAsync(
                    Encoding.UTF8.GetString(content),
                    expectedManifestVersion);

                if (!written)
                {
                    Debug.LogWarning(
                        "Downloaded level manifest is invalid. Cached manifest will be preserved.");
                }

                return written;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Level manifest could not be refreshed: {exception.Message}. " +
                    "Cached manifest will be used.");
                return false;
            }
        }

        private bool TryResolveManifestSettings(
            out Uri manifestUri,
            out int? manifestVersion)
        {
            if (TryGetRemoteManifestSettings(
                    out manifestUri,
                    out int remoteManifestVersion))
            {
                manifestVersion = remoteManifestVersion;
                return true;
            }

            manifestVersion = null;
            return TryCreateManifestUri(
                _fallbackManifestUrl,
                out manifestUri);
        }

        private static bool TryGetRemoteManifestSettings(
            out Uri manifestUri,
            out int manifestVersion)
        {
            manifestUri = null;
            manifestVersion = 0;

            try
            {
                FirebaseRemoteConfig remoteConfig =
                    FirebaseRemoteConfig.DefaultInstance;
                ConfigValue versionValue = remoteConfig.GetValue(
                    FirebaseRemoteConfigKeys.LevelManifestVersion);
                ConfigValue urlValue = remoteConfig.GetValue(
                    FirebaseRemoteConfigKeys.LevelManifestUrl);

                if (versionValue.Source != ValueSource.RemoteValue ||
                    urlValue.Source != ValueSource.RemoteValue)
                {
                    return false;
                }

                long remoteVersion = versionValue.LongValue;

                if (remoteVersion <= 0 ||
                    remoteVersion > int.MaxValue ||
                    !TryCreateManifestUri(
                        urlValue.StringValue,
                        out manifestUri))
                {
                    Debug.LogWarning(
                        "Remote level manifest settings are invalid.");
                    return false;
                }

                manifestVersion = (int)remoteVersion;
                return true;
            }
            catch (FormatException)
            {
                Debug.LogWarning(
                    "Remote level manifest version is invalid.");
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Remote level settings could not be read: {exception.Message}");
                return false;
            }
        }

        private static bool TryCreateManifestUri(
            string manifestUrl,
            out Uri manifestUri)
        {
            return Uri.TryCreate(
                       manifestUrl,
                       UriKind.Absolute,
                       out manifestUri) &&
                   manifestUri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
