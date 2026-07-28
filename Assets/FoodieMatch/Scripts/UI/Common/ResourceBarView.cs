using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.Reward;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Common
{
    public sealed class ResourceBarView : MonoBehaviour
    {
        [SerializeField] private CoinCounterView _coinCounterView;
        [SerializeField] private HeartCounterView _heartCounterView;
        [SerializeField] private Button _coinCounterButton;
        [SerializeField] private GameObject _addCoinButton;

        private Action _coinClicked;

        public CoinCounterView CoinCounterView => _coinCounterView;

        private void Awake()
        {
            _coinCounterButton.onClick.AddListener(OnCoinClicked);
            _coinCounterButton.enabled = false;
            _addCoinButton.SetActive(false);
        }

        private void OnDestroy()
        {
            _coinCounterButton.onClick.RemoveListener(OnCoinClicked);
        }

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            SetCoinBalance(coinBalance);
            SetHeartStatus(heartStatus);
        }

        public void SetCoinBalance(long coinBalance)
        {
            _coinCounterView.SetCoinBalance(coinBalance);
        }

        public void SetHeartStatus(HeartStatus heartStatus)
        {
            _heartCounterView.SetHeartStatus(heartStatus);
        }

        public void SetResourceClickActions(
            Action coinClicked,
            Action heartClicked)
        {
            _coinClicked = coinClicked;
            _coinCounterButton.enabled = coinClicked != null;
            _addCoinButton.SetActive(coinClicked != null);
            _heartCounterView.SetClickAction(heartClicked);
        }

        public void Clear()
        {
            SetResourceClickActions(null, null);
            _heartCounterView.Clear();
        }

        private void OnCoinClicked()
        {
            _coinClicked();
        }
    }
}
