using System.Threading.Tasks;

namespace FoodieMatch.Core.Application.Purchasing
{
    public interface IStorePaymentGateway
    {
        Task<StorePaymentResult> PurchaseAsync(string storeProductId);
    }
}
