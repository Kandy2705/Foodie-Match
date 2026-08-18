using System;

namespace FoodieMatch.UI.Debugging
{
    public sealed class PlayerDebugPopupViewActions
    {
        public PlayerDebugPopupViewActions(
            Action closeClicked,
            Action<DebugMenuValues> applyClicked,
            Action resetGoldPassClaimHistoryClicked)
        {
            CloseClicked = closeClicked ??
                throw new ArgumentNullException(nameof(closeClicked));
            ApplyClicked = applyClicked ??
                throw new ArgumentNullException(nameof(applyClicked));
            ResetGoldPassClaimHistoryClicked =
                resetGoldPassClaimHistoryClicked ??
                throw new ArgumentNullException(
                    nameof(resetGoldPassClaimHistoryClicked));
        }

        public Action CloseClicked { get; }

        public Action<DebugMenuValues> ApplyClicked { get; }

        public Action ResetGoldPassClaimHistoryClicked { get; }
    }
}
