using UnityEngine;

namespace FoodieMatch.UI.ClaimReward
{
    public sealed class ClaimRewardItemData
    {
        public ClaimRewardItemData(Sprite icon, string amountText)
        {
            Icon = icon;
            AmountText = amountText;
        }

        public Sprite Icon { get; }

        public string AmountText { get; }
    }
}
