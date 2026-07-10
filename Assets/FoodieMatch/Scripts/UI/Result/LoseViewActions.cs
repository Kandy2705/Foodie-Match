using System;

namespace FoodieMatch.UI.Result
{
    public sealed class LoseViewActions
    {
        public LoseViewActions(Action tryAgainClicked, Action homeClicked)
        {
            TryAgainClicked = tryAgainClicked ?? throw new ArgumentNullException(nameof(tryAgainClicked));
            HomeClicked = homeClicked ?? throw new ArgumentNullException(nameof(homeClicked));
        }

        public Action TryAgainClicked { get; }

        public Action HomeClicked { get; }
    }
}
