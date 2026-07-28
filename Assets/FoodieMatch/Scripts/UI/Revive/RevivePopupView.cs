using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Revive
{
    public sealed class RevivePopupView : PopupBase, IPlayerResourceView
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _freeAdsButton;
        [SerializeField] private Button _playOnButton;
        [SerializeField] private PopupAnimController _popupAnimController;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private ResourceBarView _resourceBarView;

        private Action _closeClicked;
        private Action _freeAdsClicked;
        private Action _playOnClicked;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _freeAdsButton.onClick.AddListener(OnFreeAdsButtonClicked);
            _playOnButton.onClick.AddListener(OnPlayOnButtonClicked);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            _freeAdsButton.onClick.RemoveListener(OnFreeAdsButtonClicked);
            _playOnButton.onClick.RemoveListener(OnPlayOnButtonClicked);
        }

        public void SetActions(RevivePopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _freeAdsClicked = actions.FreeAdsClicked;
            _playOnClicked = actions.PlayOnClicked;
        }

        public void SetPlayerResources(
            long coinBalance,
            HeartStatus heartStatus)
        {
            _resourceBarView.SetPlayerResources(coinBalance, heartStatus);
        }

        public void SetResourceClickActions(
            Action coinClicked,
            Action heartClicked)
        {
            _resourceBarView.SetResourceClickActions(
                coinClicked,
                heartClicked);
        }

        public void SetCost(string costText)
        {
            _costText.text = costText;
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
            _playOnClicked = null;
            _resourceBarView.Clear();

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

        private void OnPlayOnButtonClicked()
        {
            _playOnClicked?.Invoke();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

    }
}
