using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FoodieMatch.UI.AddressableAssets
{
    public interface IAddressableUiFactory
    {
        Task<T> GetOrCreateAsync<T>(
            string address,
            Transform parent,
            CancellationToken cancellationToken = default)
            where T : Component;

        bool TryGetCached<T>(string address, out T instance)
            where T : Component;

        void Release(string address);

        void ReleaseAll();
    }
}
