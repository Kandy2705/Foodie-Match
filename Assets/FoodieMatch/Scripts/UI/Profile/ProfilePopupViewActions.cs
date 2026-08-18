using System;

namespace FoodieMatch.UI.Profile
{
    public sealed class ProfilePopupViewActions
    {
        public ProfilePopupViewActions(
            Action closeClicked,
            Action editAvatarClicked = null)
        {
            CloseClicked = closeClicked ??
                throw new ArgumentNullException(nameof(closeClicked));
            EditAvatarClicked = editAvatarClicked;
        }

        public Action CloseClicked { get; }

        public Action EditAvatarClicked { get; }
    }
}
