using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Packages
{
    [DisallowMultipleComponent]
    public abstract class PackagePopupViewBase : PopupBase
    {
        [Header("Package Buttons")]
        [SerializeField] protected Button _closeButton;
        [SerializeField] protected Button _buyButton;

        [Header("Animation")]
        [SerializeField] protected PopupAnimController _popupAnimController;

        protected Func<Task<ShopPurchaseResult>> _buyClicked;
        protected Action _closeClicked;

        protected virtual void Awake()
        {
            _popupAnimController ??= GetComponent<PopupAnimController>();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyButtonClicked);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            }
        }

        public virtual void SetActions(PackagePopupViewActions actions)
        {
            if (actions == null)
            {
                return;
            }

            _buyClicked = actions.BuyClicked;
            _closeClicked = actions.CloseClicked;
        }

        public override void Show()
        {
            base.Show();

            if (_popupAnimController != null)
            {
                _popupAnimController.Open();
            }
        }

        public override void Hide()
        {
            if (gameObject.activeInHierarchy && _popupAnimController != null)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _buyClicked = null;
            _closeClicked = null;
            base.Dispose();
        }

        protected virtual void OnCloseButtonClicked()
        {
            if (_closeClicked != null)
            {
                _closeClicked();
            }
            else
            {
                RequestHide();
            }
        }

        protected virtual async void OnBuyButtonClicked()
        {
            if (_buyClicked == null)
            {
                return;
            }

            if (_buyButton != null)
            {
                _buyButton.interactable = false;
            }

            try
            {
                ShopPurchaseResult result = await _buyClicked();
                if (result != null && result.IsSuccess)
                {
                    OnPurchaseSucceeded(result);
                }
                else
                {
                    OnPurchaseFailed(result?.ErrorMessage ?? "Purchase failed.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                OnPurchaseFailed(exception.Message);
            }
            finally
            {
                if (this != null && _buyButton != null)
                {
                    _buyButton.interactable = true;
                }
            }
        }

        protected virtual void OnPurchaseSucceeded(ShopPurchaseResult result)
        {
            RequestHide();
        }

        protected virtual void OnPurchaseFailed(string errorMessage)
        {
            Debug.LogWarning($"[{GetType().Name}] Purchase failed: {errorMessage}", this);
        }

        protected virtual void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
