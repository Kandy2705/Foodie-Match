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
        private const float CardOvershootScale = 1.1f;
        private const float CardStaggerFraction = 0.5f;

        [SerializeField] private ResourceBarView _resourceBarView;
        [SerializeField] private CoinCounterView _coinCounterView;
        [SerializeField] private HeartCounterView _heartCounterView;
        [SerializeField] private Button _coinCounterButton;
        [SerializeField] private GameObject _addCoinButton;
        [SerializeField] private Button _closeButton;

        [Header("Card Reveal")]
        [SerializeField] private RectTransform[] _sectionHeaders;
        [SerializeField, Min(0f)] private float _cardRevealDelay = 0.2f;
        [SerializeField, Min(0f)] private float _cardScaleUpDuration = 0.18f;
        [SerializeField, Min(0f)] private float _cardSettleDuration = 0.12f;

        private readonly Dictionary<string, ShopProductCardView> _cardsByProductId =
            new(StringComparer.Ordinal);
        private IGameShopConfig _shopConfig;
        private Func<string, Task<ShopPurchaseResult>> _purchaseHandler;
        private Transform[] _revealTargets;
        private ShopProductCardView[] _targetCards;
        private Graphic[][] _targetGraphics;
        private Vector3[] _targetVisibleScales;
        private Sequence _cardRevealSequence;
        private bool _isInitialized;
        private bool _isPopup;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
            _closeButton.gameObject.SetActive(false);
            DisableResourceActions();
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            BindCards();
        }

        private void OnDestroy()
        {
            StopCardRevealAnimation();
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
                PlayCardRevealAnimation();
            }
        }

        public override void Hide()
        {
            StopCardRevealAnimation();
            RestoreCards();
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
            PlayCardRevealAnimation();
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

            _resourceBarView ??= GetComponentInChildren<ResourceBarView>(true);
            _coinCounterView ??= GetComponentInChildren<CoinCounterView>(true);
            _heartCounterView ??= GetComponentInChildren<HeartCounterView>(true);
            _isInitialized = true;
        }

        private void BuildRevealCache(ShopProductCardView[] cards)
        {
            Dictionary<Transform, ShopProductCardView> cardsByTransform =
                new(cards.Length);

            for (int i = 0; i < cards.Length; i++)
            {
                cardsByTransform.Add(
                    cards[i].transform,
                    cards[i]);
            }

            List<Transform> revealTargets = new();
            List<ShopProductCardView> targetCards = new();

            for (int headerIndex = 0;
                 headerIndex < _sectionHeaders.Length;
                 headerIndex++)
            {
                RectTransform header = _sectionHeaders[headerIndex];
                Transform section = header.parent;

                revealTargets.Add(header);
                targetCards.Add(null);

                for (int childIndex = 0;
                     childIndex < section.childCount;
                     childIndex++)
                {
                    Transform child = section.GetChild(childIndex);

                    if (cardsByTransform.TryGetValue(
                            child,
                            out ShopProductCardView card))
                    {
                        revealTargets.Add(child);
                        targetCards.Add(card);
                    }
                }
            }

            _revealTargets = revealTargets.ToArray();
            _targetCards = targetCards.ToArray();
            _targetGraphics = new Graphic[_revealTargets.Length][];
            _targetVisibleScales = new Vector3[_revealTargets.Length];

            for (int i = 0; i < _revealTargets.Length; i++)
            {
                _targetGraphics[i] =
                    _revealTargets[i].GetComponentsInChildren<Graphic>(true);
                _targetVisibleScales[i] =
                    _revealTargets[i].localScale;
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

        private void PlayCardRevealAnimation()
        {
            EnsureInitialized();
            StopCardRevealAnimation();
            PrepareCardsForReveal();

            float revealDelay = Mathf.Max(0f, _cardRevealDelay);
            float scaleUpDuration = Mathf.Max(0f, _cardScaleUpDuration);
            float settleDuration = Mathf.Max(0f, _cardSettleDuration);
            float cardDuration = scaleUpDuration + settleDuration;
            float cardStagger = cardDuration * CardStaggerFraction;

            Sequence sequence =
                Sequence.Create(useUnscaledTime: true);

            for (int i = 0; i < _revealTargets.Length; i++)
            {
                int targetIndex = i;
                Transform revealTarget = _revealTargets[i];
                Vector3 visibleScale = _targetVisibleScales[i];
                Vector3 overshootScale =
                    visibleScale * CardOvershootScale;
                float startTime = revealDelay + cardStagger * i;

                sequence = sequence
                    .Insert(startTime, Tween.Scale(
                        revealTarget,
                        overshootScale,
                        scaleUpDuration,
                        Ease.OutQuad))
                    .Insert(startTime + scaleUpDuration, Tween.Scale(
                        revealTarget,
                        visibleScale,
                        settleDuration,
                        Ease.OutBack))
                    .InsertCallback(
                        startTime + cardDuration,
                        this,
                        view => view.EnableRevealTarget(targetIndex));

                Graphic[] graphics = _targetGraphics[i];

                for (int graphicIndex = 0;
                     graphicIndex < graphics.Length;
                     graphicIndex++)
                {
                    sequence = sequence.Insert(
                        startTime,
                        Tween.Alpha(
                            graphics[graphicIndex],
                            0f,
                            1f,
                            cardDuration,
                            Ease.Linear));
                }
            }

            _cardRevealSequence = sequence;
        }

        private void PrepareCardsForReveal()
        {
            for (int i = 0; i < _revealTargets.Length; i++)
            {
                _targetCards[i]?.SetRevealComplete(false);
                _revealTargets[i].localScale = Vector3.zero;
                SetRevealTargetAlpha(i, 0f);
            }
        }

        private void EnableRevealTarget(int targetIndex)
        {
            SetRevealTargetAlpha(targetIndex, 1f);
            _revealTargets[targetIndex].localScale =
                _targetVisibleScales[targetIndex];
            _targetCards[targetIndex]?.SetRevealComplete(true);
        }

        private void RestoreCards()
        {
            if (_revealTargets == null)
            {
                return;
            }

            for (int i = 0; i < _revealTargets.Length; i++)
            {
                SetRevealTargetAlpha(i, 1f);
                _revealTargets[i].localScale =
                    _targetVisibleScales[i];
                _targetCards[i]?.SetRevealComplete(true);
            }
        }

        private void SetRevealTargetAlpha(int targetIndex, float alpha)
        {
            Graphic[] graphics = _targetGraphics[targetIndex];

            for (int i = 0; i < graphics.Length; i++)
            {
                Color color = graphics[i].color;
                color.a = alpha;
                graphics[i].color = color;
            }
        }

        private void StopCardRevealAnimation()
        {
            if (_cardRevealSequence.isAlive)
            {
                _cardRevealSequence.Stop();
            }

            _cardRevealSequence = default;
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
