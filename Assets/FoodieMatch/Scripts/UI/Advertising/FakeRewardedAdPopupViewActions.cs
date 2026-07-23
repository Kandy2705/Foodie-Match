using System;

namespace FoodieMatch.UI.Advertising
{
    public sealed class FakeRewardedAdPopupViewActions
    {
        public FakeRewardedAdPopupViewActions(
            Action completed,
            Action cancelled)
        {
            Completed = completed ??
                throw new ArgumentNullException(nameof(completed));
            Cancelled = cancelled ??
                throw new ArgumentNullException(nameof(cancelled));
        }

        public Action Completed { get; }

        public Action Cancelled { get; }
    }
}
