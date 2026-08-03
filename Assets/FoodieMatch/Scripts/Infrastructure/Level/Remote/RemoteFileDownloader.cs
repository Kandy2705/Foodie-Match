using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace FoodieMatch.Infrastructure.Level.Remote
{
    internal static class RemoteFileDownloader
    {
        public static async Task<byte[]> DownloadAsync(
            Uri uri,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            using UnityWebRequest request =
                UnityWebRequest.Get(uri.AbsoluteUri);
            request.timeout = timeoutSeconds;
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

            return request.downloadHandler.data;
        }
    }
}
