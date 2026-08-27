using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Rewards;
using FoodieMatch.Core.Domain.Booster;
using UnityEngine;

namespace FoodieMatch.UI.DailyReward
{
    public enum DailyRewardType
    {
        Coin = 0,
        Booster = 1
    }

    [Serializable]
    public sealed class DailyFreeRewardStepDefinition
    {
        [SerializeField] private DailyRewardType _rewardType = DailyRewardType.Coin;
        [SerializeField] private BoosterType _boosterType = BoosterType.Swap;
        [SerializeField] private int _amount = 100;
        [SerializeField] private Sprite _icon;

        public DailyRewardType RewardType => _rewardType;
        public BoosterType BoosterType => _boosterType;
        public int Amount => _amount;
        public Sprite Icon => _icon;

        public DailyFreeRewardStepDefinition() { }

        public DailyFreeRewardStepDefinition(
            DailyRewardType rewardType,
            BoosterType boosterType,
            int amount,
            Sprite icon = null)
        {
            _rewardType = rewardType;
            _boosterType = boosterType;
            _amount = amount;
            _icon = icon;
        }
    }

    [Serializable]
    public sealed class DailyQuestDefinition
    {
        [SerializeField] private DailyQuestType _type;
        [SerializeField] private string _title;
        [SerializeField] private int _target = 2;
        [SerializeField] private int _coinReward = 40;
        [SerializeField] private Sprite _icon;

        public DailyQuestType Type => _type;
        public string Title => _title;
        public int Target => _target;
        public int CoinReward => _coinReward;
        public Sprite Icon => _icon;

        public DailyQuestDefinition() { }

        public DailyQuestDefinition(
            DailyQuestType type,
            string title,
            int target,
            int coinReward,
            Sprite icon = null)
        {
            _type = type;
            _title = title;
            _target = target;
            _coinReward = coinReward;
            _icon = icon;
        }
    }

    [CreateAssetMenu(
        fileName = "DailyRewardCatalog",
        menuName = "FoodieMatch/Daily Reward/Daily Reward Catalog")]
    public sealed class DailyRewardCatalogSO : ScriptableObject
    {
        [Header("General Icons")]
        [SerializeField] private Sprite _coinIcon;
        [SerializeField] private Sprite _storageIcon;
        [SerializeField] private Sprite _swapIcon;
        [SerializeField] private Sprite _plateIcon;
        [SerializeField] private Sprite _fridgeIcon;

        [Header("Daily Gift")]
        [SerializeField] private int _dailyGiftCoinAmount = 50;
        [SerializeField] private int _dailyGiftCooldownSeconds = 3600;
        [SerializeField] private Sprite _dailyGiftIcon;

        [Header("Quests")]
        [SerializeField] private List<DailyQuestDefinition> _quests = new();

        [Header("Free Rewards (Ad & Final Bonus)")]
        [SerializeField] private List<DailyFreeRewardStepDefinition> _freeRewardSteps = new();

        public Sprite CoinIcon => _coinIcon;
        public Sprite StorageIcon => _storageIcon;
        public Sprite SwapIcon => _swapIcon;
        public Sprite PlateIcon => _plateIcon;
        public Sprite FridgeIcon => _fridgeIcon;

        public int DailyGiftCoinAmount => _dailyGiftCoinAmount;
        public int DailyGiftCooldownSeconds => _dailyGiftCooldownSeconds;
        public Sprite DailyGiftIcon => _dailyGiftIcon != null ? _dailyGiftIcon : _coinIcon;

        public IReadOnlyList<DailyQuestDefinition> Quests => _quests;
        public IReadOnlyList<DailyFreeRewardStepDefinition> FreeRewardSteps => _freeRewardSteps;

        public Sprite GetBoosterIcon(BoosterType boosterType)
        {
            return boosterType switch
            {
                BoosterType.Storage => _storageIcon,
                BoosterType.Swap => _swapIcon,
                BoosterType.Plate => _plateIcon,
                BoosterType.Fridge => _fridgeIcon,
                _ => null
            };
        }

        public Sprite GetQuestIcon(DailyQuestType type)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i] != null && _quests[i].Type == type && _quests[i].Icon != null)
                {
                    return _quests[i].Icon;
                }
            }

            return _coinIcon;
        }

        public string GetQuestTitle(DailyQuestType type)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i] != null && _quests[i].Type == type && !string.IsNullOrEmpty(_quests[i].Title))
                {
                    return _quests[i].Title;
                }
            }

            return type switch
            {
                DailyQuestType.CompleteLevels => "Pass 3 levels",
                DailyQuestType.UseStorage => "Use 2 Storage",
                DailyQuestType.UseSwap => "Use 2 Refresh",
                DailyQuestType.UsePlate => "Use 2 Plate",
                DailyQuestType.UseFridge => "Use 2 Fridge",
                _ => type.ToString()
            };
        }

        public Sprite GetFreeRewardIcon(int index)
        {
            if (_freeRewardSteps != null && index >= 0 && index < _freeRewardSteps.Count)
            {
                DailyFreeRewardStepDefinition step = _freeRewardSteps[index];
                if (step != null)
                {
                    if (step.Icon != null)
                    {
                        return step.Icon;
                    }

                    if (step.RewardType == DailyRewardType.Booster)
                    {
                        return GetBoosterIcon(step.BoosterType);
                    }

                    return _coinIcon;
                }
            }

            return _coinIcon;
        }

        public int GetFreeRewardAmount(int index)
        {
            if (_freeRewardSteps != null && index >= 0 && index < _freeRewardSteps.Count)
            {
                DailyFreeRewardStepDefinition step = _freeRewardSteps[index];
                if (step != null)
                {
                    return step.Amount;
                }
            }

            return index switch
            {
                0 => 100,
                1 => 1,
                2 => 1,
                3 => 300,
                _ => 1
            };
        }
    }
}
