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

        private Action _confirmClicked;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
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

            _titleText.text = popupData.Title;
            _descriptionText.text = popupData.Description;
            ApplyIcon(popupData.Icon);
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
            _confirmClicked = null;

            base.Dispose();
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

        private void OnConfirmButtonClicked()
        {
            _confirmClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

    }
}
