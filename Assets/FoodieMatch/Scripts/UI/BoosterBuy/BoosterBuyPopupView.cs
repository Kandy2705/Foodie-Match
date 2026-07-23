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
        private bool _isClosing;

        private void Awake()
        {
            if (_popupAnimController == null)
            {
                _popupAnimController = GetComponent<PopupAnimController>();
            }

            EnsureButtonReferences();
            EnsureContentReferences();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (_freeAdsButton != null)
            {
                _freeAdsButton.onClick.AddListener(OnFreeAdsButtonClicked);
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (_freeAdsButton != null)
            {
                _freeAdsButton.onClick.RemoveListener(OnFreeAdsButtonClicked);
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            }
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
            _resourceBarView?.SetPlayerResources(
                coinBalance,
                heartStatus);
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

            EnsureContentReferences();

            UiTmpText.SetText(_titleText, popupData.Title);
            UiTmpText.SetText(_descriptionText, popupData.Description);
            UiTmpText.SetText(_costText, popupData.CostText);
            UiTmpText.SetText(_bonusAmountText, popupData.BonusAmountText);

            ApplyIcon(popupData.Icon);
        }

        private void ApplyIcon(Sprite icon)
        {
            if (_iconImage == null)
            {
                return;
            }

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
            _isClosing = false;
            base.Show();

            if (_popupAnimController != null)
            {
                _popupAnimController.Open();
            }
        }

        public override void Hide()
        {
            if (_isClosing)
            {
                return;
            }

            if (_popupAnimController != null && gameObject.activeInHierarchy)
            {
                _isClosing = true;
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
            _isClosing = false;

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
            _isClosing = false;
            base.Hide();
        }

        private void EnsureButtonReferences()
        {
            if (_closeButton == null)
            {
                _closeButton = FindChildButton("CloseButton");
            }

            if (_freeAdsButton == null)
            {
                _freeAdsButton = FindChildButton("FreeAdsButton");
            }

            if (_buyButton == null)
            {
                _buyButton = FindChildButton("PlayOnButton");
            }

            if (_buyButton == null)
            {
                _buyButton = FindChildButton("BuyButton");
            }
        }

        private void EnsureContentReferences()
        {
            if (_titleText == null)
            {
                _titleText = UiTmpText.FindChild(transform, "BoosterBuyTitleText");
            }

            if (_descriptionText == null)
            {
                _descriptionText = UiTmpText.FindChild(transform, "DescriptionText");
            }

            if (_costText == null)
            {
                _costText = UiTmpText.FindChild(transform, "CostText");
            }

            if (_bonusAmountText == null)
            {
                _bonusAmountText = UiTmpText.FindChild(transform, "BonusAmountText");
            }

            if (_iconImage == null)
            {
                _iconImage = FindChildImage("BoosterBuyIconImage");
            }
        }

        private Button FindChildButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];

                if (button != null && button.gameObject.name == objectName)
                {
                    return button;
                }
            }

            return null;
        }

        private Image FindChildImage(string objectName)
        {
            Image[] images = GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image != null && image.gameObject.name == objectName)
                {
                    return image;
                }
            }

            return null;
        }
    }
}
