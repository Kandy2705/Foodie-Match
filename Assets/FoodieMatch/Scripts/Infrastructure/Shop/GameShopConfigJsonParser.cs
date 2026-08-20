using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Domain.Booster;
using UnityEngine;

namespace FoodieMatch.Infrastructure.Shop
{
    public sealed class GameShopConfigJsonParser
    {
        public bool TryParse(
            string json,
            out IGameShopConfig config,
            out string errorMessage)
        {
            try
            {
                ShopCatalogDto catalogDto =
                    JsonUtility.FromJson<ShopCatalogDto>(json);

                if (catalogDto?.products == null)
                {
                    throw new ArgumentException(
                        "Shop config products are missing.");
                }

                List<ShopProductDefinition> products = new(
                    catalogDto.products.Length);

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

        private static ShopProductDefinition MapProduct(
            ShopProductDto productDto)
        {
            if (productDto == null || productDto.rewards == null)
            {
                throw new ArgumentException(
                    "Shop product rewards are missing.");
            }

            Dictionary<BoosterType, int> boosterAmounts = new();

            if (productDto.rewards.boosters != null)
            {
                for (int i = 0; i < productDto.rewards.boosters.Length; i++)
                {
                    BoosterRewardDto boosterDto =
                        productDto.rewards.boosters[i];

                    if (boosterDto == null ||
                        !Enum.TryParse(
                            boosterDto.type,
                            ignoreCase: false,
                            out BoosterType boosterType) ||
                        !Enum.IsDefined(typeof(BoosterType), boosterType) ||
                        !boosterAmounts.TryAdd(
                            boosterType,
                            boosterDto.amount))
                    {
                        throw new ArgumentException(
                            "Shop product booster rewards are invalid.");
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
    }
}
