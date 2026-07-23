using FoodieMatch.Core.Domain.Booster;
using FoodieMatch.UI.Booster;
using FoodieMatch.UI.Popup;
using UnityEngine;

namespace FoodieMatch.UI.BoosterBuy
{
    public sealed class BoosterBuyPopupData : IPopupData
    {
        public BoosterBuyPopupData(
            BoosterType boosterType,
            string title,
            string description,
            Sprite icon,
            string costText,
            string bonusAmountText)
        {
            BoosterType = boosterType;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Icon = icon;
            CostText = costText ?? string.Empty;
            BonusAmountText = bonusAmountText ?? string.Empty;
        }

        public BoosterType BoosterType { get; }

        public string Title { get; }

        public string Description { get; }

        public Sprite Icon { get; }

        public string CostText { get; }

        public string BonusAmountText { get; }

        public static BoosterBuyPopupData FromCatalogEntry(
            BoosterBuyContentEntry entry,
            string costText = null,
            string bonusAmountText = null)
        {
            if (entry == null)
            {
                return null;
            }

            return new BoosterBuyPopupData(
                entry.BoosterType,
                entry.Title,
                entry.Description,
                entry.Icon,
                costText ?? entry.DefaultCostText,
                bonusAmountText ?? entry.DefaultBonusAmountText);
        }
    }
}
