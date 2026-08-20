using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.UI.Packages;

namespace FoodieMatch.UI.StarterPack
{
    public sealed class StarterPackPopupViewActions : PackagePopupViewActions
    {
        public StarterPackPopupViewActions(
            Func<Task<ShopPurchaseResult>> buyClicked,
            Action closeClicked = null)
            : base(buyClicked, closeClicked)
        {
        }
    }
}
