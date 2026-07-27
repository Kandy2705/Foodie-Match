using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace FoodieMatch.UI.Reward
{
    public class CoinCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _coinCountText;
        [SerializeField] private RectTransform _coinTarget;

        public RectTransform CoinTarget => _coinTarget;

        public void SetCoinBalance(long coinBalance)
        {
            long displayedBalance = Math.Max(0, coinBalance);
            _coinCountText.text = displayedBalance.ToString(
                "N0",
                CultureInfo.InvariantCulture);
        }
    }
}
