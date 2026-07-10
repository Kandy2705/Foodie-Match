using System;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudViewActions
    {
        public GameplayHudViewActions(Action pauseClicked)
        {
            PauseClicked = pauseClicked ?? throw new ArgumentNullException(nameof(pauseClicked));
        }

        public Action PauseClicked { get; }
    }
}
