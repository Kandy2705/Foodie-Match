using System.Collections.Generic;

namespace FoodieMatch.Core.Application.Configuration.Shop
{
    public sealed class GameShopConfigSession : IGameShopConfig
    {
        private IGameShopConfig _current;

        public GameShopConfigSession(IGameShopConfig initial)
        {
            _current = initial;
        }

        public IReadOnlyList<ShopProductDefinition> Products =>
            _current.Products;

        public bool TryGetProduct(
            string productId,
            out ShopProductDefinition product)
        {
            return _current.TryGetProduct(productId, out product);
        }

        public void Apply(IGameShopConfig config)
        {
            _current = config;
        }
    }
}
