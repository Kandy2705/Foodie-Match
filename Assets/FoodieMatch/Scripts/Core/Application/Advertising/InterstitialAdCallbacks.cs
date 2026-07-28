using System;

namespace FoodieMatch.Core.Application.Advertising
{
    public readonly struct InterstitialAdCallbacks
    {
        public InterstitialAdCallbacks(
            Action displayed,
            Action closed,
            Action displayFailed)
        {
            Displayed = displayed;
            Closed = closed;
            DisplayFailed = displayFailed;
        }

        public Action Displayed { get; }

        public Action Closed { get; }

        public Action DisplayFailed { get; }
    }
}
