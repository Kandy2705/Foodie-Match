using System;
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
            _coinCountText.text = Math.Max(0, coinBalance).ToString();
        }
    }
}
