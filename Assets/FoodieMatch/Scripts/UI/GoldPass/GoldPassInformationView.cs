using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassInformationView : PopupBase
    {
        [SerializeField] private Button _backgroundButton;
        [SerializeField] private PopupAnimController _animController;

        private void Awake()
        {
            _backgroundButton.onClick.AddListener(OnBackgroundClicked);
        }

        private void OnDestroy()
        {
            _backgroundButton.onClick.RemoveListener(OnBackgroundClicked);
        }

        public override void Show()
        {
            base.Show();
            _animController.Open();
        }

        public override void Hide()
        {
            if (gameObject.activeInHierarchy)
            {
                _animController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        private void OnBackgroundClicked()
        {
            RequestHide();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
