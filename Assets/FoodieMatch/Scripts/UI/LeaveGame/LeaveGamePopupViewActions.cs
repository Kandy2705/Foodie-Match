using System;

namespace FoodieMatch.UI.LeaveGame
{
    public sealed class LeaveGamePopupViewActions
    {
        public LeaveGamePopupViewActions(Action closeClicked, Action leaveClicked)
        {
            CloseClicked = closeClicked ?? throw new ArgumentNullException(nameof(closeClicked));
            LeaveClicked = leaveClicked ?? throw new ArgumentNullException(nameof(leaveClicked));
        }

        public Action CloseClicked { get; }

        public Action LeaveClicked { get; }
    }
}
