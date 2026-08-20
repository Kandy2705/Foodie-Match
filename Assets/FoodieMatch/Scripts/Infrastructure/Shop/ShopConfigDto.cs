using System;

namespace FoodieMatch.Infrastructure.Shop
{
    [Serializable]
    internal sealed class ShopCatalogDto
    {
        public ShopProductDto[] products;
    }

    [Serializable]
    internal sealed class ShopProductDto
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
    internal sealed class ShopRewardsDto
    {
        public long coins;
        public long unlimitedHeartSeconds;
        public bool removeAds;
        public BoosterRewardDto[] boosters;
    }

    [Serializable]
    internal sealed class BoosterRewardDto
    {
        public string type;
        public int amount;
    }
}
