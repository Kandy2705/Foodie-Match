using System;

namespace FoodieMatch.UI.BoosterGuide
{
    public sealed class BoosterGuidePopupViewActions
    {
        public BoosterGuidePopupViewActions(Action closeClicked, Action confirmClicked)
        {
            ConfirmClicked = confirmClicked ?? throw new ArgumentNullException(nameof(confirmClicked));
        }

        public Action ConfirmClicked { get; }
    }
}
