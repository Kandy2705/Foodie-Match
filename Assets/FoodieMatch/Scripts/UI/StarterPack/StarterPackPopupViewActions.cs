using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;

namespace FoodieMatch.UI.StarterPack
{
    public sealed class StarterPackPopupViewActions
    {
        public StarterPackPopupViewActions(
            Func<Task<ShopPurchaseResult>> buyClicked)
        {
            BuyClicked = buyClicked ??
                throw new ArgumentNullException(nameof(buyClicked));
        }

        public Func<Task<ShopPurchaseResult>> BuyClicked { get; }
    }
}
