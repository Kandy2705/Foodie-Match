using System;
using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Domain.Booster;
using UnityEngine;

namespace FoodieMatch.UI.GoldPass
{
    [CreateAssetMenu(
        fileName = "GoldPassRewardVisualCatalog",
        menuName = "FoodieMatch/Gold Pass/Reward Visual Catalog")]
    public sealed class GoldPassRewardVisualCatalogSO : ScriptableObject
    {
        [Header("Resources")]
        [SerializeField] private Sprite _coinIcon;
        [SerializeField] private Sprite _unlimitedHeartIcon;

        [Header("Boosters")]
        [SerializeField] private Sprite _plateIcon;
        [SerializeField] private Sprite _storageIcon;
        [SerializeField] private Sprite _swapIcon;
        [SerializeField] private Sprite _fridgeIcon;

        [Header("Treasures")]
        [SerializeField] private Sprite _treasure1Icon;
        [SerializeField] private Sprite _treasure2Icon;
        [SerializeField] private Sprite _treasure3Icon;

        public Sprite GetIcon(GoldPassRewardDefinition reward)
        {
            switch (reward.Type)
            {
                case GoldPassRewardType.Coin:
                    return _coinIcon;
                case GoldPassRewardType.UnlimitedHeart:
                    return _unlimitedHeartIcon;
                case GoldPassRewardType.Booster:
                    return GetBoosterIcon(reward.BoosterType.Value);
                case GoldPassRewardType.Treasure1:
                    return _treasure1Icon;
                case GoldPassRewardType.Treasure2:
                    return _treasure2Icon;
                case GoldPassRewardType.Treasure3:
                    return _treasure3Icon;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reward));
            }
        }

        private Sprite GetBoosterIcon(BoosterType boosterType)
        {
            switch (boosterType)
            {
                case BoosterType.Plate:
                    return _plateIcon;
                case BoosterType.Storage:
                    return _storageIcon;
                case BoosterType.Swap:
                    return _swapIcon;
                case BoosterType.Fridge:
                    return _fridgeIcon;
                default:
                    throw new ArgumentOutOfRangeException(nameof(boosterType));
            }
        }
    }
}
