using System;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public sealed class GoldPassPurchaseDefinition
    {
        public GoldPassPurchaseDefinition(
            string storeProductId,
            string fallbackDisplayPrice)
        {
            StoreProductId = RequireValue(
                storeProductId,
                nameof(storeProductId));
            FallbackDisplayPrice = RequireValue(
                fallbackDisplayPrice,
                nameof(fallbackDisplayPrice));
        }

        public string StoreProductId { get; }

        public string FallbackDisplayPrice { get; }

        private static string RequireValue(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A Gold Pass purchase value is required.",
                    parameterName);
            }

            return value;
        }
    }
}
