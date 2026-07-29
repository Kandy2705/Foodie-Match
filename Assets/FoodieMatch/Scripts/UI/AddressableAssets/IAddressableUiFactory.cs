using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FoodieMatch.UI.AddressableAssets
{
    public interface IAddressableUiFactory
    {
        event Action<bool> LoadingStateChanged;

        event Action<float> LoadingProgressChanged;

        Task PreloadLabelAsync(
            string label,
            CancellationToken cancellationToken = default);

        Task<T> GetOrCreateAsync<T>(
            string address,
            Transform parent,
            CancellationToken cancellationToken = default)
            where T : Component;

        Task<T> GetOrCreateAsync<T>(
            string address,
            string instanceKey,
            Transform parent,
            CancellationToken cancellationToken = default)
            where T : Component;

        bool TryGetCached<T>(string instanceKey, out T instance)
            where T : Component;

        void Release(string instanceKey);

        void ReleaseLabel(string label);

        void ReleaseAll();
    }
}
