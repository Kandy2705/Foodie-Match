using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Domain.Booster;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Shop
{
    public sealed class ResourcesGameShopConfigLoader
    {
        private const string ResourcePath = "Shop/shop_bundles";

        public bool TryLoad(out IGameShopConfig config, out string errorMessage)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);

            if (asset == null)
            {
                config = null;
                errorMessage = $"Shop config resource {ResourcePath} was not found.";
                return false;
            }

            try
            {
                ShopCatalogDto catalogDto = JsonUtility.FromJson<ShopCatalogDto>(asset.text);

                if (catalogDto?.products == null)
                {
                    throw new ArgumentException("Shop config products are missing.");
                }

                List<ShopProductDefinition> products = new(catalogDto.products.Length);

                for (int i = 0; i < catalogDto.products.Length; i++)
                {
                    products.Add(MapProduct(catalogDto.products[i]));
                }

                config = new GameShopConfigSnapshot(products);
                errorMessage = null;
                return true;
            }
            catch (Exception exception)
            {
                config = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        private static ShopProductDefinition MapProduct(ShopProductDto productDto)
        {
            if (productDto == null || productDto.rewards == null)
            {
                throw new ArgumentException("Shop product rewards are missing.");
            }

            Dictionary<BoosterType, int> boosterAmounts = new();

            if (productDto.rewards.boosters != null)
            {
                for (int i = 0; i < productDto.rewards.boosters.Length; i++)
                {
                    BoosterRewardDto boosterDto = productDto.rewards.boosters[i];

                    if (boosterDto == null ||
                        !Enum.TryParse(boosterDto.type, ignoreCase: false, out BoosterType boosterType) ||
                        !Enum.IsDefined(typeof(BoosterType), boosterType) ||
                        !boosterAmounts.TryAdd(boosterType, boosterDto.amount))
                    {
                        throw new ArgumentException("Shop product booster rewards are invalid.");
                    }
                }
            }

            return new ShopProductDefinition(
                productDto.id,
                productDto.displayName,
                productDto.storeProductId,
                productDto.fallbackDisplayPrice,
                productDto.cardType,
                productDto.section,
                productDto.sortOrder,
                new ShopRewardDefinition(
                    productDto.rewards.coins,
                    productDto.rewards.unlimitedHeartSeconds,
                    productDto.rewards.removeAds,
                    boosterAmounts));
        }

        [Serializable]
        private sealed class ShopCatalogDto
        {
            public ShopProductDto[] products;
        }

        [Serializable]
        private sealed class ShopProductDto
        {
            public string id;
            public string displayName;
            public string storeProductId;
            public string fallbackDisplayPrice;
            public string cardType;
            public string section;
            public int sortOrder;
            public ShopRewardsDto rewards;
        }

        [Serializable]
        private sealed class ShopRewardsDto
        {
            public long coins;
            public long unlimitedHeartSeconds;
            public bool removeAds;
            public BoosterRewardDto[] boosters;
        }

        [Serializable]
        private sealed class BoosterRewardDto
        {
            public string type;
            public int amount;
        }
    }
}
