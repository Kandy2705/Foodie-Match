using System;
using FoodieMatch.Core.Application.Player;

namespace FoodieMatch.UI.Debugging
{
    public sealed class PlayerDebugPopupViewActions
    {
        public PlayerDebugPopupViewActions(
            Action closeClicked,
            Action<PlayerProfileDebugUpdate> applyClicked)
        {
            CloseClicked = closeClicked ??
                throw new ArgumentNullException(nameof(closeClicked));
            ApplyClicked = applyClicked ??
                throw new ArgumentNullException(nameof(applyClicked));
        }

        public Action CloseClicked { get; }

        public Action<PlayerProfileDebugUpdate> ApplyClicked { get; }
    }
}
