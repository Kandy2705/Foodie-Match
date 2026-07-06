using System;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeViewActions
    {
        public HomeViewActions(Action playClicked)
        {
            PlayClicked = playClicked ?? throw new ArgumentNullException(nameof(playClicked));
        }

        public Action PlayClicked { get; }
    }
}
