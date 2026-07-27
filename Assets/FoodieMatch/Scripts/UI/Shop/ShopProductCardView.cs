using System;
using System.Collections.Generic;
using FoodieMatch.Core.Application.Configuration.Shop;
using FoodieMatch.Core.Domain.Booster;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Shop
{
    public sealed class ShopProductCardView : MonoBehaviour
    {
        private static readonly BoosterType[] VisualBoosterTypes =
        {
            BoosterType.Plate,
            BoosterType.Storage,
            BoosterType.Swap,
            BoosterType.Fridge
        };

        [SerializeField] private string _productId;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TMP_Text _packNameText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _coinRewardText;
        [SerializeField] private GameObject _unlimitedHeartRoot;
        [SerializeField] private TMP_Text _unlimitedHeartText;
        [SerializeField] private GameObject _noAdsRoot;
        [SerializeField] private ShopBoosterRewardSlotView[] _boosterRewardSlots;

        private ShopProductDefinition _boundProduct;

        public event Action<string> PurchaseRequested;

        public string ProductId => _productId;

        private void Awake()
        {
            EnsureReferences();
            _buyButton?.onClick.AddListener(OnBuyButtonClicked);
        }

        private void OnEnable()
        {
            ApplyBoundProduct();
        }

        private void OnDestroy()
        {
            _buyButton?.onClick.RemoveListener(OnBuyButtonClicked);
        }

        public void SetProductId(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException(
                    "A shop product id is required.",
                    nameof(productId));
            }

            _productId = productId;
        }

        public void Bind(ShopProductDefinition product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            if (!string.Equals(_productId, product.Id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The shop product does not match this card.");
            }

            _boundProduct = product;
            EnsureReferences();
            ApplyBoundProduct();
        }

        public void SetBusy(bool isBusy)
        {
            if (_buyButton != null)
            {
                _buyButton.interactable = !isBusy;
            }
        }

        public void Clear()
        {
        }

        public void ConfigureEditorReferences()
        {
            EnsureReferences();

            if (_boosterRewardSlots == null || _boosterRewardSlots.Length == 0)
            {
                Transform listRoot = FindDescendant(transform, "BoosterRewardList");

                if (listRoot != null)
                {
                    List<ShopBoosterRewardSlotView> slots = new();

                    for (int i = 0; i < listRoot.childCount &&
                                    i < VisualBoosterTypes.Length; i++)
                    {
                        Transform child = listRoot.GetChild(i);
                        ShopBoosterRewardSlotView slot =
                            child.GetComponent<ShopBoosterRewardSlotView>() ??
                            child.gameObject.AddComponent<ShopBoosterRewardSlotView>();
                        slot.Configure(VisualBoosterTypes[i]);
                        slots.Add(slot);
                    }

                    _boosterRewardSlots = slots.ToArray();
                }
            }
        }

        private void ApplyBoundProduct()
        {
            if (_boundProduct == null)
            {
                return;
            }

            if (_packNameText != null && !string.IsNullOrWhiteSpace(_boundProduct.DisplayName))
            {
                _packNameText.text = _boundProduct.DisplayName;
            }

            if (_priceText != null)
            {
                _priceText.text = _boundProduct.FallbackDisplayPrice;
            }

            if (_coinRewardText != null)
            {
                _coinRewardText.text = _boundProduct.Rewards.Coins.ToString();
            }

            bool hasUnlimitedHeart =
                _boundProduct.Rewards.UnlimitedHeartSeconds > 0;

            if (_unlimitedHeartRoot != null)
            {
                _unlimitedHeartRoot.SetActive(hasUnlimitedHeart);
            }

            if (hasUnlimitedHeart && _unlimitedHeartText != null)
            {
                _unlimitedHeartText.text = FormatDuration(
                    _boundProduct.Rewards.UnlimitedHeartSeconds);
            }

            if (_noAdsRoot != null)
            {
                _noAdsRoot.SetActive(_boundProduct.Rewards.RemoveAds);
            }

            if (_boosterRewardSlots == null)
            {
                return;
            }

            for (int i = 0; i < _boosterRewardSlots.Length; i++)
            {
                ShopBoosterRewardSlotView slot = _boosterRewardSlots[i];

                if (slot == null)
                {
                    continue;
                }

                _boundProduct.Rewards.BoosterAmounts.TryGetValue(
                    slot.BoosterType,
                    out int amount);
                slot.Bind(amount);
            }
        }

        private void EnsureReferences()
        {
            _buyButton ??= GetComponentInChildren<Button>(true);
            _packNameText ??= FindText("PackNameText");
            _coinRewardText ??= FindText("CoinAmountText") ??
                               FindText("GoldAmountText");
            _unlimitedHeartRoot ??= FindDescendant(
                transform,
                "UnlimitedHeartReward")?.gameObject;
            _unlimitedHeartText ??= FindText("DurationText");
            _noAdsRoot ??= FindDescendant(transform, "NoAdsReward")?.gameObject;

            if (_priceText == null && _buyButton != null)
            {
                _priceText = _buyButton.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private TMP_Text FindText(string transformName)
        {
            Transform target = FindDescendant(transform, transformName);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }

        private void OnBuyButtonClicked()
        {
            if (!string.IsNullOrWhiteSpace(_productId))
            {
                PurchaseRequested?.Invoke(_productId);
            }
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), targetName);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string FormatDuration(long totalSeconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(totalSeconds);

            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h";
            }

            return $"{Math.Max(1, (int)duration.TotalMinutes)}m";
        }
    }
}
