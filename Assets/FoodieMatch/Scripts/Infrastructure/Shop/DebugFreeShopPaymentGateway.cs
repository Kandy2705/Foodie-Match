using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;

namespace FoodieMatch.Infrastructure.Shop
{
    public sealed class DebugFreeShopPaymentGateway : IShopPaymentGateway
    {
        public Task<ShopPaymentResult> PurchaseAsync(string storeProductId)
        {
            if (string.IsNullOrWhiteSpace(storeProductId))
            {
                throw new ArgumentException(
                    "A store product id is required.",
                    nameof(storeProductId));
            }

            return Task.FromResult(ShopPaymentResult.Succeeded());
        }
    }
}
