using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.StarterPack
{
    [DisallowMultipleComponent]
    public sealed class StarterPackPopupView : PopupBase
    {
        private const string CloseButtonName = "CloseButton";

        [SerializeField] private Button _closeButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private void Awake()
        {
            _closeButton ??= FindRequiredButton(CloseButtonName);
            _popupAnimController ??=
                GetComponent<PopupAnimController>();

            if (_popupAnimController == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(StarterPackPopupView)} requires a " +
                    $"{nameof(PopupAnimController)} on its root.");
            }

            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
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

        private Button FindRequiredButton(string buttonName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == buttonName)
                {
                    return buttons[i];
                }
            }

            throw new InvalidOperationException(
                $"{nameof(StarterPackPopupView)} could not find " +
                $"{buttonName}.");
        }

        private void OnCloseButtonClicked()
        {
            RequestHide();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
