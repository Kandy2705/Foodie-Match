using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Application.Configuration.Shop
{
    public sealed class GameShopConfigSnapshot : IGameShopConfig
    {
        private readonly ReadOnlyCollection<ShopProductDefinition> _products;
        private readonly ReadOnlyDictionary<string, ShopProductDefinition> _productsById;

        public GameShopConfigSnapshot(IReadOnlyList<ShopProductDefinition> products)
        {
            if (products == null || products.Count == 0)
            {
                throw new ArgumentException(
                    "At least one shop product is required.",
                    nameof(products));
            }

            List<ShopProductDefinition> copiedProducts = new(products.Count);
            Dictionary<string, ShopProductDefinition> productsById = new(
                StringComparer.Ordinal);

            for (int i = 0; i < products.Count; i++)
            {
                ShopProductDefinition product = products[i] ??
                    throw new ArgumentException(
                        "Shop products cannot contain null values.",
                        nameof(products));

                if (!productsById.TryAdd(product.Id, product))
                {
                    throw new ArgumentException(
                        $"Shop product id {product.Id} is duplicated.",
                        nameof(products));
                }

                copiedProducts.Add(product);
            }

            _products = new ReadOnlyCollection<ShopProductDefinition>(copiedProducts);
            _productsById = new ReadOnlyDictionary<string, ShopProductDefinition>(
                productsById);
        }

        public IReadOnlyList<ShopProductDefinition> Products => _products;

        public bool TryGetProduct(string productId, out ShopProductDefinition product)
        {
            product = null;

            return !string.IsNullOrWhiteSpace(productId) &&
                   _productsById.TryGetValue(productId, out product);
        }
    }
}
