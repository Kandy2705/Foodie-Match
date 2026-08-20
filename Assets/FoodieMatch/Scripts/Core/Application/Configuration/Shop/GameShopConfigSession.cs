using System.Collections.Generic;

namespace FoodieMatch.Core.Application.Configuration.Shop
{
    public sealed class GameShopConfigSession : IGameShopConfig
    {
        private readonly string[] _supportedProductIds;
        private IGameShopConfig _current;

        public GameShopConfigSession(IGameShopConfig initial)
        {
            _current = initial;
            _supportedProductIds = new string[initial.Products.Count];

            for (int i = 0; i < initial.Products.Count; i++)
            {
                _supportedProductIds[i] = initial.Products[i].Id;
            }
        }

        public IReadOnlyList<ShopProductDefinition> Products =>
            _current.Products;

        public bool TryGetProduct(
            string productId,
            out ShopProductDefinition product)
        {
            return _current.TryGetProduct(productId, out product);
        }

        public bool TryApply(IGameShopConfig config)
        {
            if (config.Products.Count != _supportedProductIds.Length)
            {
                return false;
            }

            for (int i = 0; i < _supportedProductIds.Length; i++)
            {
                if (!config.TryGetProduct(_supportedProductIds[i], out _))
                {
                    return false;
                }
            }

            _current = config;
            return true;
        }
    }
}
