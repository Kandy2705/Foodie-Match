using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.MainMenu;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.Reward;
using UnityEngine;

namespace FoodieMatch.UI.Shop
{
    public sealed class ShopView : MonoBehaviour, IPlayerResourceView, IMainMenuViewLifecycle
    {
        [SerializeField] private ResourceBarView _resourceBarView;
        [SerializeField] private CoinCounterView _coinCounterView;
        [SerializeField] private HeartCounterView _heartCounterView;

        private readonly Dictionary<string, ShopProductCardView> _cardsByProductId =
            new(StringComparer.Ordinal);
        private IGameShopConfig _shopConfig;
        private Func<string, Task<ShopPurchaseResult>> _purchaseHandler;
        private bool _isInitialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            BindCards();
        }

        private void OnDestroy()
        {
            UnsubscribeCards();
        }

        public void SetPurchaseHandler(
            Func<string, Task<ShopPurchaseResult>> purchaseHandler)
        {
            _purchaseHandler = purchaseHandler;
        }

        public void Bind(IGameShopConfig shopConfig)
        {
            _shopConfig = shopConfig ?? throw new ArgumentNullException(nameof(shopConfig));
            EnsureInitialized();
            BindCards();
        }

        public void SetPlayerResources(long coinBalance, HeartStatus heartStatus)
        {
            _resourceBarView ??= GetComponentInChildren<ResourceBarView>(true);

            if (_resourceBarView != null)
            {
                _resourceBarView.SetPlayerResources(coinBalance, heartStatus);
                return;
            }

            _coinCounterView ??= GetComponentInChildren<CoinCounterView>(true);
            _heartCounterView ??= GetComponentInChildren<HeartCounterView>(true);
            _coinCounterView?.SetCoinBalance(coinBalance);
            _heartCounterView?.SetHeartStatus(heartStatus);
        }

        public void Clear()
        {
        }

        private void EnsureInitialized()
        {
            if (_isInitialized && _cardsByProductId.Count > 0)
            {
                return;
            }

            UnsubscribeCards();
            _cardsByProductId.Clear();

            ShopProductCardView[] cards =
                GetComponentsInChildren<ShopProductCardView>(true);

            for (int i = 0; i < cards.Length; i++)
            {
                ShopProductCardView card = cards[i];

                if (card == null || string.IsNullOrWhiteSpace(card.ProductId))
                {
                    Debug.LogError("Shop card is missing a ProductId.", card);
                    continue;
                }

                if (!_cardsByProductId.TryAdd(card.ProductId, card))
                {
                    Debug.LogError(
                        $"Shop contains duplicate ProductId {card.ProductId}.",
                        card);
                    continue;
                }

                card.PurchaseRequested += OnPurchaseRequested;
            }

            _resourceBarView ??= GetComponentInChildren<ResourceBarView>(true);
            _coinCounterView ??= GetComponentInChildren<CoinCounterView>(true);
            _heartCounterView ??= GetComponentInChildren<HeartCounterView>(true);
            _isInitialized = true;
        }

        private void BindCards()
        {
            if (_shopConfig == null)
            {
                return;
            }

            EnsureInitialized();

            for (int i = 0; i < _shopConfig.Products.Count; i++)
            {
                ShopProductDefinition product = _shopConfig.Products[i];

                if (!_cardsByProductId.TryGetValue(product.Id, out ShopProductCardView card))
                {
                    Debug.LogError($"Shop card for {product.Id} was not found.", this);
                    continue;
                }

                card.Bind(product);
            }
        }

        private async void OnPurchaseRequested(string productId)
        {
            EnsureInitialized();

            if (!_cardsByProductId.TryGetValue(productId, out ShopProductCardView card))
            {
                return;
            }

            card.SetBusy(true);

            try
            {
                if (_purchaseHandler != null)
                {
                    await _purchaseHandler(productId);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                card.SetBusy(false);
            }
        }

        private void UnsubscribeCards()
        {
            foreach (ShopProductCardView card in _cardsByProductId.Values)
            {
                if (card != null)
                {
                    card.PurchaseRequested -= OnPurchaseRequested;
                }
            }
        }
    }
}
