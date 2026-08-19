using System;
using System.Threading.Tasks;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassPurchaseViewActions
    {
        public GoldPassPurchaseViewActions(
            Func<Task> buyClicked,
            Action seasonExpired)
        {
            BuyClicked = buyClicked;
            SeasonExpired = seasonExpired;
        }

        public Func<Task> BuyClicked { get; }

        public Action SeasonExpired { get; }
    }
}
