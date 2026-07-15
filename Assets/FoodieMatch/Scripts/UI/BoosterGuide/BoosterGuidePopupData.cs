using FoodieMatch.Data.Booster;
using FoodieMatch.UI.Popup;
using UnityEngine;

namespace FoodieMatch.UI.BoosterGuide
{
    public sealed class BoosterGuidePopupData : IPopupData
    {
        public BoosterGuidePopupData(
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

        public static BoosterGuidePopupData FromCatalogEntry(
            BoosterGuideContentEntry entry,
            string costText = null,
            string bonusAmountText = null)
        {
            if (entry == null)
            {
                return null;
            }

            return new BoosterGuidePopupData(
                entry.BoosterType,
                entry.Title,
                entry.Description,
                entry.Icon,
                costText ?? entry.DefaultCostText,
                bonusAmountText ?? entry.DefaultBonusAmountText);
        }
    }
}
