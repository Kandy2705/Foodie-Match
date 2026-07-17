using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.BoosterGuide
{
    public sealed class BoosterGuidePopupView : PopupBase
    {
        private const float IconNativeSizeScale = 1.4f;

        [SerializeField] private Button _confirmButton;
        [SerializeField] private PopupAnimController _popupAnimController;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Image _iconImage;

        private Action _closeClicked;
        private Action _confirmClicked;
        private bool _isClosing;

        private void Awake()
        {
            if (_popupAnimController == null)
            {
                _popupAnimController = GetComponent<PopupAnimController>();
            }

            EnsureButtonReferences();
            EnsureContentReferences();

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
            }
        }

        public void SetActions(BoosterGuidePopupViewActions actions)
        {
            _confirmClicked = actions.ConfirmClicked;
        }

        public override void Setup(IPopupData data)
        {
            if (data is not BoosterGuidePopupData popupData)
            {
                return;
            }

            ApplyContent(popupData);
        }

        public void ApplyContent(BoosterGuidePopupData popupData)
        {
            if (popupData == null)
            {
                return;
            }

            EnsureContentReferences();

            UiTmpText.SetText(_titleText, popupData.Title);
            UiTmpText.SetText(_descriptionText, popupData.Description);
            ApplyIcon(popupData.Icon);
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
            _confirmClicked = null;
            _isClosing = false;

            base.Dispose();
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

        private void OnCloseButtonClicked()
        {
            _closeClicked?.Invoke();
        }

        private void OnConfirmButtonClicked()
        {
            _confirmClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            _isClosing = false;
            base.Hide();
        }

        private void EnsureButtonReferences()
        {
            if (_confirmButton == null)
            {
                _confirmButton = FindChildButton("PlayOnButton");
            }

            if (_confirmButton == null)
            {
                _confirmButton = FindChildButton("ConfirmButton");
            }
        }

        private void EnsureContentReferences()
        {
            if (_titleText == null)
            {
                _titleText = UiTmpText.FindChild(transform, "BoosterBuyTitleText");
            }

            if (_titleText == null)
            {
                _titleText = UiTmpText.FindChild(transform, "BoosterGuideTitleText");
            }

            if (_descriptionText == null)
            {
                _descriptionText = UiTmpText.FindChild(transform, "DescriptionText");
            }

            if (_iconImage == null)
            {
                _iconImage = FindChildImage("BoosterBuyIconImage");
            }

            if (_iconImage == null)
            {
                _iconImage = FindChildImage("BoosterGuideIconImage");
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
