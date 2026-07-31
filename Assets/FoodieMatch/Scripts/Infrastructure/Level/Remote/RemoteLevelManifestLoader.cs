using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.RemoteConfig;
using FoodieMatch.Infrastructure.RemoteConfig;
using UnityEngine;
using UnityEngine.Networking;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    public sealed class RemoteLevelManifestLoader
    {
        private const int DownloadTimeoutSeconds = 4;

        private readonly RemoteLevelManifestCache _cache;

        public RemoteLevelManifestLoader(RemoteLevelManifestCache cache)
        {
            _cache = cache;
        }

        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            FirebaseRemoteConfig remoteConfig =
                FirebaseRemoteConfig.DefaultInstance;

            if (!TryGetManifestSettings(
                    remoteConfig,
                    out int manifestVersion,
                    out string manifestUrl))
            {
                return;
            }

            if (_cache.TryLoad(out RemoteLevelManifestDto cachedManifest) &&
                cachedManifest.ManifestVersion == manifestVersion)
            {
                return;
            }

            try
            {
                string content = await DownloadManifestAsync(
                    manifestUrl,
                    cancellationToken);
                bool written = await _cache.WriteAtomicallyAsync(
                    content,
                    manifestVersion);

                if (!written)
                {
                    Debug.LogWarning(
                        "Downloaded level manifest is invalid. Cached manifest will be preserved.");
                }
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
            }
        }

        private static bool TryGetManifestSettings(
            FirebaseRemoteConfig remoteConfig,
            out int manifestVersion,
            out string manifestUrl)
        {
            ConfigValue versionValue = remoteConfig.GetValue(
                FirebaseRemoteConfigKeys.LevelManifestVersion);
            ConfigValue urlValue = remoteConfig.GetValue(
                FirebaseRemoteConfigKeys.LevelManifestUrl);

            manifestVersion = 0;
            manifestUrl = string.Empty;

            if (versionValue.Source != ValueSource.RemoteValue ||
                urlValue.Source != ValueSource.RemoteValue)
            {
                return false;
            }

            try
            {
                long remoteVersion = versionValue.LongValue;
                manifestUrl = urlValue.StringValue;

                if (remoteVersion == 0 &&
                    string.IsNullOrWhiteSpace(manifestUrl))
                {
                    return false;
                }

                if (remoteVersion <= 0 ||
                    remoteVersion > int.MaxValue ||
                    !IsValidManifestUrl(manifestUrl))
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
        }

        private static bool IsValidManifestUrl(string manifestUrl)
        {
            return Uri.TryCreate(
                       manifestUrl,
                       UriKind.Absolute,
                       out Uri uri) &&
                   uri.Scheme == Uri.UriSchemeHttps;
        }

        private static async Task<string> DownloadManifestAsync(
            string manifestUrl,
            CancellationToken cancellationToken)
        {
            using UnityWebRequest request =
                UnityWebRequest.Get(manifestUrl);
            request.timeout = DownloadTimeoutSeconds;
            UnityWebRequestAsyncOperation operation =
                request.SendWebRequest();

            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(request.error);
            }

            return request.downloadHandler.text;
        }
    }
}
