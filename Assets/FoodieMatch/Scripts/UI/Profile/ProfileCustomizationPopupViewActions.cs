using System;

namespace FoodieMatch.UI.Profile
{
    public sealed class ProfileCustomizationPopupViewActions
    {
        public ProfileCustomizationPopupViewActions(
            Action<string, string, string> saveClicked,
            Action closeClicked)
        {
            SaveClicked = saveClicked ??
                throw new ArgumentNullException(nameof(saveClicked));
            CloseClicked = closeClicked ??
                throw new ArgumentNullException(nameof(closeClicked));
        }

        public Action<string, string, string> SaveClicked { get; }

        public Action CloseClicked { get; }
    }
}
