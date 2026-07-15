using System;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudViewActions
    {
        public GameplayHudViewActions(
            Action pauseClicked,
            Action<int> boosterClicked = null)
        {
            PauseClicked = pauseClicked ?? throw new ArgumentNullException(nameof(pauseClicked));
            BoosterClicked = boosterClicked;
        }

        public Action PauseClicked { get; }

        public Action<int> BoosterClicked { get; }
    }
}
