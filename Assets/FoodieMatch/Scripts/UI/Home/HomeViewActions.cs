using System;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeViewActions
    {
        public HomeViewActions(
            Action playClicked,
            Action settingClicked)
        {
            PlayClicked = playClicked ?? throw new ArgumentNullException(nameof(playClicked));
            SettingClicked = settingClicked ?? throw new ArgumentNullException(nameof(settingClicked));
        }

        public Action PlayClicked { get; }

        public Action SettingClicked { get; }
    }
}
