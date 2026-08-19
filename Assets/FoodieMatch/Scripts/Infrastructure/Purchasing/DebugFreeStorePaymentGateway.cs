using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Purchasing;

namespace FoodieMatch.Infrastructure.Purchasing
{
    public sealed class DebugFreeStorePaymentGateway : IStorePaymentGateway
    {
        public Task<StorePaymentResult> PurchaseAsync(string storeProductId)
        {
            if (string.IsNullOrWhiteSpace(storeProductId))
            {
                throw new ArgumentException(
                    "A store product id is required.",
                    nameof(storeProductId));
            }

            return Task.FromResult(StorePaymentResult.Succeeded());
        }
    }
}
