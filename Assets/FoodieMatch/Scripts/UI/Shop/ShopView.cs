using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.MainMenu;
using FoodieMatch.UI.Home;
using FoodieMatch.UI.Popup;
using FoodieMatch.UI.Reward;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Shop
{
    public sealed class ShopView :
        PopupBase,
        IPlayerResourceView,
        IMainMenuViewLifecycle,
        IMainMenuTabSelectionHandler
    {
        private const string StarterPackSection = "StarterPack";

        [SerializeField] private CoinCounterView _coinCounterView;
        [SerializeField] private HeartCounterView _heartCounterView;
        [SerializeField] private Button _coinCounterButton;
        [SerializeField] private GameObject _addCoinButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private ScrollRect _shopScrollView;

        [Header("Item Reveal")]
        [SerializeField] private Vector2 _itemRevealStartOffset = new(0f, -80f);
        [SerializeField, Range(0f, 1f)] private float _itemRevealStartScaleMultiplier = 0.9f;
        [SerializeField, Min(0f)] private float _itemRevealDelay = 0.1f;
        [SerializeField, Min(0f)] private float _itemRevealDuration = 0.3f;
        [SerializeField, Min(0f)] private float _itemRevealInterval = 0.08f;
        [SerializeField] private Ease _itemRevealMoveEase = Ease.OutBack;
        [SerializeField] private Ease _itemRevealScaleEase = Ease.OutBack;
        [SerializeField] private Ease _itemRevealFadeEase = Ease.OutCubic;

        private readonly Dictionary<string, ShopProductCardView> _cardsByProductId =
            new(StringComparer.Ordinal);
        private IGameShopConfig _shopConfig;
        private Func<string, Task<ShopPurchaseResult>> _purchaseHandler;
        private ShopRevealItemView[] _revealItems;
        private Sequence _itemRevealSequence;
        private bool _isInitialized;
        private bool _isPopup;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
            _closeButton.gameObject.SetActive(false);
            DisableResourceActions();
            EnsureInitialized();
            PrepareRevealItems();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            BindCards();
        }

        private void OnDestroy()
        {
            StopItemRevealAnimation();
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            UnsubscribeCards();
        }

        public override void Setup(IPopupData data)
        {
            _isPopup = data is ShopPopupData;
        }

        public override void Show()
        {
            base.Show();
            _closeButton.gameObject.SetActive(_isPopup);

            if (_isPopup)
            {
                PlayItemRevealAnimation();
            }
        }

        public override void Hide()
        {
            StopItemRevealAnimation();
            RestoreRevealItems();
            _isPopup = false;
            _closeButton.gameObject.SetActive(false);
            base.Hide();
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
            _coinCounterView ??= GetComponentInChildren<CoinCounterView>(true);
            _heartCounterView ??= GetComponentInChildren<HeartCounterView>(true);
            _coinCounterView?.SetCoinBalance(coinBalance);
            _heartCounterView?.SetHeartStatus(heartStatus);
        }

        public void SetResourceClickActions(
            Action coinClicked,
            Action heartClicked)
        {
            DisableResourceActions();
        }

        public void Clear()
        {
        }

        public void OnTabSelected()
        {
            PlayItemRevealAnimation();
        }

        private void DisableResourceActions()
        {
            _coinCounterButton.enabled = false;
            _addCoinButton.SetActive(false);
            _heartCounterView.SetClickAction(null);
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
            BuildRevealCache(cards);

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

            _coinCounterView ??= GetComponentInChildren<CoinCounterView>(true);
            _heartCounterView ??= GetComponentInChildren<HeartCounterView>(true);
            _isInitialized = true;
        }

        private void BuildRevealCache(ShopProductCardView[] cards)
        {
            _revealItems = GetComponentsInChildren<ShopRevealItemView>(true);
            Dictionary<Transform, ShopProductCardView> cardsByTransform = new(cards.Length);

            for (int i = 0; i < cards.Length; i++)
            {
                cardsByTransform.Add(cards[i].transform, cards[i]);
            }

            for (int i = 0; i < _revealItems.Length; i++)
            {
                cardsByTransform.TryGetValue(
                    _revealItems[i].transform,
                    out ShopProductCardView productCard);
                _revealItems[i].Initialize(productCard);
            }
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

                if (string.Equals(
                        product.Section,
                        StarterPackSection,
                        StringComparison.Ordinal))
                {
                    continue;
                }

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

        private void PlayItemRevealAnimation()
        {
            EnsureInitialized();
            StopItemRevealAnimation();
            ResetScrollPosition();
            PrepareRevealItems();

            Sequence sequence = Sequence.Create(useUnscaledTime: true);

            for (int i = 0; i < _revealItems.Length; i++)
            {
                float startTime = _itemRevealDelay + _itemRevealInterval * i;
                sequence = _revealItems[i].InsertReveal(
                    sequence,
                    startTime,
                    _itemRevealDuration,
                    _itemRevealMoveEase,
                    _itemRevealScaleEase,
                    _itemRevealFadeEase);
            }

            _itemRevealSequence = sequence;
        }

        private void ResetScrollPosition()
        {
            _shopScrollView.StopMovement();
            _shopScrollView.verticalNormalizedPosition = 1f;
        }

        private void PrepareRevealItems()
        {
            for (int i = 0; i < _revealItems.Length; i++)
            {
                _revealItems[i].Prepare(
                    _itemRevealStartOffset,
                    _itemRevealStartScaleMultiplier);
            }
        }

        private void RestoreRevealItems()
        {
            for (int i = 0; i < _revealItems.Length; i++)
            {
                _revealItems[i].Restore();
            }
        }

        private void StopItemRevealAnimation()
        {
            if (_itemRevealSequence.isAlive)
            {
                _itemRevealSequence.Stop();
            }

            _itemRevealSequence = default;
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

        private void OnCloseClicked()
        {
            RequestHide();
        }
    }

    internal sealed class ShopPopupData : IPopupData
    {
        public static ShopPopupData Instance { get; } = new();

        private ShopPopupData()
        {
        }
    }
}
