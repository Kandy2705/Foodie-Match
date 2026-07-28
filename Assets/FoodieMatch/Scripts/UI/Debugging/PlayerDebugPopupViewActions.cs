using System;

namespace FoodieMatch.UI.Debugging
{
    public sealed class PlayerDebugPopupViewActions
    {
        public PlayerDebugPopupViewActions(
            Action closeClicked,
            Action<DebugMenuValues> applyClicked)
        {
            CloseClicked = closeClicked ??
                throw new ArgumentNullException(nameof(closeClicked));
            ApplyClicked = applyClicked ??
                throw new ArgumentNullException(nameof(applyClicked));
        }

        public Action CloseClicked { get; }

        public Action<DebugMenuValues> ApplyClicked { get; }
    }
}
