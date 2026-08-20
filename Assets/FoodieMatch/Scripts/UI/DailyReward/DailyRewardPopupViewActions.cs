using System;

namespace FoodieMatch.UI.DailyReward
{
    public sealed class DailyRewardPopupViewActions
    {
        public DailyRewardPopupViewActions(Action closeClicked)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
        }

        public Action CloseClicked { get; }
    }
}
