using System;
using FoodieMatch.UI.Common;
using TMPro;
using UnityEngine;

namespace FoodieMatch.UI.Reward
{
    public class CoinCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _coinCountText;
        [SerializeField] private RectTransform _coinTarget;

        public RectTransform CoinTarget
        {
            get
            {
                EnsureCoinTargetReference();
                return _coinTarget;
            }
        }

        public void SetCoinBalance(long coinBalance)
        {
            UiTmpText.SetText(_coinCountText, Math.Max(0, coinBalance).ToString());
        }

        private void EnsureCoinTargetReference()
        {
            if (_coinTarget != null)
            {
                return;
            }

            RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);

            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];

                if (rectTransform != null && rectTransform.gameObject.name == "CoinIconImage")
                {
                    _coinTarget = rectTransform;
                    return;
                }
            }
        }
    }
}
