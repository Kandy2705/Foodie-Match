using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.BoosterBuy
{
    public sealed class BoosterBuyPopupView : PopupBase, IPlayerResourceView
    {
        private const float IconNativeSizeScale = 1.4f;

        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _freeAdsButton;
        [SerializeField] private Button _buyButton;
        [SerializeField] private PopupAnimController _popupAnimController;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _bonusAmountText;
        [SerializeField] private ResourceBarView _resourceBarView;

        private Action _closeClicked;
        private Action _freeAdsClicked;
        private Action _buyClicked;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _freeAdsButton.onClick.AddListener(OnFreeAdsButtonClicked);
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _freeAdsButton.onClick.RemoveListener(OnFreeAdsButtonClicked);
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }

        public void SetActions(BoosterBuyPopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _freeAdsClicked = actions.FreeAdsClicked;
            _buyClicked = actions.BuyClicked;
        }

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            _resourceBarView.SetPlayerResources(coinBalance, heartStatus);
        }

        public override void Setup(IPopupData data)
        {
            if (data is not BoosterBuyPopupData popupData)
            {
                return;
            }

            ApplyContent(popupData);
        }

        public void ApplyContent(BoosterBuyPopupData popupData)
        {
            if (popupData == null)
            {
                return;
            }

            _titleText.text = popupData.Title;
            _descriptionText.text = popupData.Description;
            _costText.text = popupData.CostText;
            _bonusAmountText.text = popupData.BonusAmountText;

            ApplyIcon(popupData.Icon);
        }

        private void ApplyIcon(Sprite icon)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;

            if (icon == null)
            {
                return;
            }

            _iconImage.SetNativeSize();

            RectTransform iconRect = _iconImage.rectTransform;
            Vector2 nativeSize = iconRect.sizeDelta;
            iconRect.sizeDelta = nativeSize * IconNativeSizeScale;
        }

        public override void Show()
        {
            base.Show();

            _popupAnimController.Open();
        }

        public override void Hide()
        {
            if (gameObject.activeInHierarchy)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _closeClicked = null;
            _freeAdsClicked = null;
            _buyClicked = null;

            base.Dispose();
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
        }

        private void OnFreeAdsButtonClicked()
        {
            _freeAdsClicked?.Invoke();
        }

        private void OnBuyButtonClicked()
        {
            _buyClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

    }
}
