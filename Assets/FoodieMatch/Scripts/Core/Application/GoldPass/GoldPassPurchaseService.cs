using System;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.Purchasing;

namespace FoodieMatch.Core.Application.GoldPass
{
    public sealed class GoldPassPurchaseService
    {
        private readonly SemaphoreSlim _purchaseGate = new(1, 1);
        private readonly IGameGoldPassConfig _config;
        private readonly IStorePaymentGateway _paymentGateway;
        private readonly GoldPassService _goldPassService;

        public GoldPassPurchaseService(
            IGameGoldPassConfig config,
            IStorePaymentGateway paymentGateway,
            GoldPassService goldPassService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _paymentGateway = paymentGateway ??
                throw new ArgumentNullException(nameof(paymentGateway));
            _goldPassService = goldPassService ??
                throw new ArgumentNullException(nameof(goldPassService));
        }

        public string FallbackDisplayPrice =>
            _config.Purchase.FallbackDisplayPrice;

        public async Task<StorePaymentResult> PurchaseAsync()
        {
            if (!await _purchaseGate.WaitAsync(0))
            {
                return StorePaymentResult.Failed(
                    "A Gold Pass purchase is already in progress.");
            }

            try
            {
                if (_goldPassService.GetStatus().IsSeasonPassPurchased)
                {
                    return StorePaymentResult.Failed(
                        "Season Pass is already active.");
                }

                StorePaymentResult paymentResult =
                    await _paymentGateway.PurchaseAsync(
                        _config.Purchase.StoreProductId);

                if (paymentResult == null || !paymentResult.IsSuccess)
                {
                    return paymentResult ?? StorePaymentResult.Failed(
                        "Payment was not completed.");
                }

                if (!await _goldPassService.ActivateSeasonPassAsync())
                {
                    return StorePaymentResult.Failed(
                        "Season Pass could not be saved.");
                }

                return paymentResult;
            }
            catch (Exception exception)
            {
                return StorePaymentResult.Failed(exception.Message);
            }
            finally
            {
                _purchaseGate.Release();
            }
        }
    }
}
