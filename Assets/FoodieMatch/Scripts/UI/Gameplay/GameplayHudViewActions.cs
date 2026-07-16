using System;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class GameplayHudViewActions
    {
        public GameplayHudViewActions(
            Action pauseClicked,
            Action<int> boosterUseClicked = null,
            Action<int> boosterAddClicked = null)
        {
            PauseClicked = pauseClicked ?? throw new ArgumentNullException(nameof(pauseClicked));
            BoosterUseClicked = boosterUseClicked;
            BoosterAddClicked = boosterAddClicked;
        }

        public Action PauseClicked { get; }

        public Action<int> BoosterUseClicked { get; }

        public Action<int> BoosterAddClicked { get; }
    }
}
