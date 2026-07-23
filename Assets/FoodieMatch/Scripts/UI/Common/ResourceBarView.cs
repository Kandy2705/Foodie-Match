using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.Reward;
using UnityEngine;

namespace FoodieMatch.UI.Common
{
    public sealed class ResourceBarView : MonoBehaviour
    {
        [SerializeField] private CoinCounterView _coinCounterView;
        [SerializeField] private HeartCounterView _heartCounterView;

        public CoinCounterView CoinCounterView => _coinCounterView;

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            SetCoinBalance(coinBalance);
            SetHeartStatus(heartStatus);
        }

        public void SetCoinBalance(long coinBalance)
        {
            _coinCounterView?.SetCoinBalance(coinBalance);
        }

        public void SetHeartStatus(HeartStatus heartStatus)
        {
            _heartCounterView?.SetHeartStatus(heartStatus);
        }

        public void Clear()
        {
            _heartCounterView?.Clear();
        }
    }
}
