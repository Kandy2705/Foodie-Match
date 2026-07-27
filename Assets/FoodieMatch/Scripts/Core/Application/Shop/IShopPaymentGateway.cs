using System.Threading.Tasks;

namespace FoodieMatch.Core.Application.Shop
{
    public interface IShopPaymentGateway
    {
        Task<ShopPaymentResult> PurchaseAsync(string storeProductId);
    }
}
