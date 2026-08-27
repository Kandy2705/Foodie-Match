using System;

namespace FoodieMatch.UI.DailyReward
{
    public sealed class DailyRewardPopupViewActions
    {
        public DailyRewardPopupViewActions(
            Action closeClicked,
            Action<int> questClicked,
            Action dailyGiftClicked,
            Action<int> freeRewardClicked,
            Action dayReset)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
            QuestClicked = questClicked ?? throw new ArgumentNullException(nameof(questClicked));
            DailyGiftClicked = dailyGiftClicked ?? throw new ArgumentNullException(nameof(dailyGiftClicked));
            FreeRewardClicked = freeRewardClicked ?? throw new ArgumentNullException(nameof(freeRewardClicked));
            DayReset = dayReset ?? throw new ArgumentNullException(nameof(dayReset));
        }

        public Action CloseClicked { get; }

        public Action<int> QuestClicked { get; }

        public Action DailyGiftClicked { get; }

        public Action<int> FreeRewardClicked { get; }

        public Action DayReset { get; }
    }
}
