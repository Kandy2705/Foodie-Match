using System;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Result
{
    public sealed class LoseView : PopupBase
    {
        [SerializeField] private Button _tryAgainButton;
        [SerializeField] private Button _homeButton;

        private Action _tryAgainClicked;
        private Action _homeClicked;

        private void Awake()
        {
            if (_tryAgainButton != null)
            {
                _tryAgainButton.onClick.AddListener(OnTryAgainButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.AddListener(OnHomeButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_tryAgainButton != null)
            {
                _tryAgainButton.onClick.RemoveListener(OnTryAgainButtonClicked);
            }

            if (_homeButton != null)
            {
                _homeButton.onClick.RemoveListener(OnHomeButtonClicked);
            }
        }

        public void SetActions(LoseViewActions actions)
        {
            _tryAgainClicked = actions.TryAgainClicked;
            _homeClicked = actions.HomeClicked;
        }

        public override void Dispose()
        {
            _tryAgainClicked = null;
            _homeClicked = null;

            base.Dispose();
        }

        private void OnTryAgainButtonClicked()
        {
            _tryAgainClicked?.Invoke();
        }

        private void OnHomeButtonClicked()
        {
            _homeClicked?.Invoke();
        }
    }
}
