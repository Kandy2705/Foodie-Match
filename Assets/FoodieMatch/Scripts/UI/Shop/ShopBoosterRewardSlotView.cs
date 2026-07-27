using System;
using FoodieMatch.Core.Domain.Booster;
using TMPro;
using UnityEngine;

namespace FoodieMatch.UI.Shop
{
    public sealed class ShopBoosterRewardSlotView : MonoBehaviour
    {
        [SerializeField] private BoosterType _boosterType;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private GameObject _root;

        public BoosterType BoosterType => _boosterType;

        public void Bind(int amount)
        {
            bool hasReward = amount > 0;

            if (_root != null)
            {
                _root.SetActive(hasReward);
            }

            if (hasReward && _amountText != null)
            {
                _amountText.text = $"x{amount}";
            }
        }

        public void Configure(BoosterType boosterType)
        {
            if (!Enum.IsDefined(typeof(BoosterType), boosterType))
            {
                throw new ArgumentOutOfRangeException(nameof(boosterType));
            }

            _boosterType = boosterType;
            _root ??= gameObject;
            _amountText ??= GetComponentInChildren<TMP_Text>(true);
        }
    }
}
