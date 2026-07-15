using System;
using UnityEngine;

namespace FoodieMatch.Data.Booster
{
    [Serializable]
    public sealed class BoosterGuideContentEntry
    {
        [SerializeField] private BoosterType _boosterType;
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _defaultCostText = "80";
        [SerializeField] private string _defaultBonusAmountText = "+1";

        public BoosterType BoosterType => _boosterType;

        public string Title => _title;

        public string Description => _description;

        public Sprite Icon => _icon;

        public string DefaultCostText => _defaultCostText;

        public string DefaultBonusAmountText => _defaultBonusAmountText;
    }
}
