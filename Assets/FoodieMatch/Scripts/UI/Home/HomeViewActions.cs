using System;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeViewActions
    {
        public HomeViewActions(
            Action playClicked,
            Action settingClicked,
            Action starterPackClicked,
            Action coinClicked,
            Action heartClicked)
        {
            PlayClicked = playClicked ??
                throw new ArgumentNullException(nameof(playClicked));
            SettingClicked = settingClicked ??
                throw new ArgumentNullException(nameof(settingClicked));
            StarterPackClicked = starterPackClicked ??
                throw new ArgumentNullException(nameof(starterPackClicked));
            CoinClicked = coinClicked ??
                throw new ArgumentNullException(nameof(coinClicked));
            HeartClicked = heartClicked ??
                throw new ArgumentNullException(nameof(heartClicked));
        }

        public Action PlayClicked { get; }

        public Action SettingClicked { get; }

        public Action StarterPackClicked { get; }

        public Action CoinClicked { get; }

        public Action HeartClicked { get; }
    }
}
