using System;

namespace FoodieMatch.Core.Application.Advertising
{
    public readonly struct RewardedAdCallbacks
    {
        public RewardedAdCallbacks(
            Action rewarded,
            Action displayed,
            Action closed,
            Action displayFailed)
        {
            Rewarded = rewarded;
            Displayed = displayed;
            Closed = closed;
            DisplayFailed = displayFailed;
        }

        public Action Rewarded { get; }

        public Action Displayed { get; }

        public Action Closed { get; }

        public Action DisplayFailed { get; }
    }
}
