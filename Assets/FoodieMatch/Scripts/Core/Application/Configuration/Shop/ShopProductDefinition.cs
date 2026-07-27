using System;

namespace FoodieMatch.Core.Application.Configuration.Shop
{
    public sealed class ShopProductDefinition
    {
        public ShopProductDefinition(
            string id,
            string displayName,
            string storeProductId,
            string fallbackDisplayPrice,
            string cardType,
            string section,
            int sortOrder,
            ShopRewardDefinition rewards)
        {
            Id = RequireValue(id, nameof(id));
            DisplayName = displayName ?? string.Empty;
            StoreProductId = RequireValue(storeProductId, nameof(storeProductId));
            FallbackDisplayPrice = RequireValue(
                fallbackDisplayPrice,
                nameof(fallbackDisplayPrice));
            CardType = RequireValue(cardType, nameof(cardType));
            Section = RequireValue(section, nameof(section));

            if (sortOrder < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sortOrder),
                    sortOrder,
                    "Shop product sort order cannot be negative.");
            }

            SortOrder = sortOrder;
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string StoreProductId { get; }

        public string FallbackDisplayPrice { get; }

        public string CardType { get; }

        public string Section { get; }

        public int SortOrder { get; }

        public ShopRewardDefinition Rewards { get; }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A shop product value is required.",
                    parameterName);
            }

            return value;
        }
    }
}
