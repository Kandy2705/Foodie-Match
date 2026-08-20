using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.UI.Packages;

namespace FoodieMatch.UI.RemoveAds
{
    public sealed class RemoveAdsPopupViewActions : PackagePopupViewActions
    {
        public RemoveAdsPopupViewActions(
            Func<Task<ShopPurchaseResult>> buyClicked,
            Action closeClicked = null)
            : base(buyClicked, closeClicked)
        {
        }
    }
}
