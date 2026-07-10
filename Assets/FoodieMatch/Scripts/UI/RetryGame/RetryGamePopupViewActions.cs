using System;

namespace FoodieMatch.UI.RetryGame
{
    public sealed class RetryGamePopupViewActions
    {
        public RetryGamePopupViewActions(Action closeClicked, Action retryClicked)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
            RetryClicked = retryClicked ?? throw new ArgumentNullException(nameof(retryClicked));
        }

        public Action CloseClicked { get; }

        public Action RetryClicked { get; }
    }
}
