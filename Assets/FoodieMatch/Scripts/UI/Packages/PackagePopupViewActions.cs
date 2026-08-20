using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;

namespace FoodieMatch.UI.Packages
{
    public class PackagePopupViewActions
    {
        public PackagePopupViewActions(
            Func<Task<ShopPurchaseResult>> buyClicked,
            Action closeClicked = null)
        {
            BuyClicked = buyClicked ?? throw new ArgumentNullException(nameof(buyClicked));
            CloseClicked = closeClicked;
        }

        public Func<Task<ShopPurchaseResult>> BuyClicked { get; }
        public Action CloseClicked { get; }
    }
}
