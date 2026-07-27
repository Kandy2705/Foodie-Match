using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Application.Player;

namespace FoodieMatch.Core.Application.Shop
{
    public sealed class ShopPurchaseService
    {
        private readonly object _pendingLock = new();
        private readonly HashSet<string> _pendingProductIds = new(
            StringComparer.Ordinal);
        private readonly IGameShopConfig _shopConfig;
        private readonly IShopPaymentGateway _paymentGateway;
        private readonly PlayerProfileService _playerProfileService;

        public ShopPurchaseService(
            IGameShopConfig shopConfig,
            IShopPaymentGateway paymentGateway,
            PlayerProfileService playerProfileService)
        {
            _shopConfig = shopConfig ?? throw new ArgumentNullException(nameof(shopConfig));
            _paymentGateway = paymentGateway ??
                throw new ArgumentNullException(nameof(paymentGateway));
            _playerProfileService = playerProfileService ??
                throw new ArgumentNullException(nameof(playerProfileService));
        }

        public async Task<ShopPurchaseResult> PurchaseAsync(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return ShopPurchaseResult.Failed("A shop product id is required.");
            }

            lock (_pendingLock)
            {
                if (!_pendingProductIds.Add(productId))
                {
                    return ShopPurchaseResult.Failed(
                        "This product purchase is already in progress.");
                }
            }

            try
            {
                if (!_shopConfig.TryGetProduct(productId, out ShopProductDefinition product))
                {
                    return ShopPurchaseResult.Failed("Shop product was not found.");
                }

                ShopPaymentResult paymentResult =
                    await _paymentGateway.PurchaseAsync(product.StoreProductId);

                if (paymentResult == null || !paymentResult.IsSuccess)
                {
                    return ShopPurchaseResult.Failed(
                        paymentResult?.ErrorMessage ?? "Payment was not completed.");
                }

                ShopRewardApplyResult applyResult =
                    await _playerProfileService.ApplyShopRewardsAsync(product.Rewards);

                return ShopPurchaseResult.Succeeded(applyResult);
            }
            catch (Exception exception)
            {
                return ShopPurchaseResult.Failed(exception.Message);
            }
            finally
            {
                lock (_pendingLock)
                {
                    _pendingProductIds.Remove(productId);
                }
            }
        }
    }
}
