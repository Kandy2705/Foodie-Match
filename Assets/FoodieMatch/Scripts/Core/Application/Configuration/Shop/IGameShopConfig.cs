using System.Collections.Generic;

namespace FoodieMatch.Core.Application.Configuration.Shop
{
    public interface IGameShopConfig
    {
        IReadOnlyList<ShopProductDefinition> Products { get; }

        bool TryGetProduct(string productId, out ShopProductDefinition product);
    }
}
