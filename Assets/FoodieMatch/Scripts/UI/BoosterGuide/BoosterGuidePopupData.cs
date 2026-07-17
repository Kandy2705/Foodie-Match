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
            Sprite icon)
        {
            BoosterType = boosterType;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Icon = icon;
        }

        public BoosterType BoosterType { get; }

        public string Title { get; }

        public string Description { get; }

        public Sprite Icon { get; }

        public static BoosterGuidePopupData FromCatalogEntry(BoosterBuyContentEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            return new BoosterGuidePopupData(
                entry.BoosterType,
                entry.Title,
                entry.Description,
                entry.Icon);
        }
    }
}
