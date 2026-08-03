using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.StarterPack
{
    [DisallowMultipleComponent]
    public sealed class StarterPackPopupView : PopupBase
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _buyButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Func<Task<ShopPurchaseResult>> _buyClicked;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }

        public void SetActions(StarterPackPopupViewActions actions)
        {
            _buyClicked = actions.BuyClicked;
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
            _buyClicked = null;
            base.Dispose();
        }

        private void OnCloseButtonClicked()
        {
            RequestHide();
        }

        private async void OnBuyButtonClicked()
        {
            _buyButton.interactable = false;

            try
            {
                await _buyClicked();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                if (this != null)
                {
                    _buyButton.interactable = true;
                }
            }
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
