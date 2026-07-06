using System;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Home
{
    public sealed class HomeView : PopupBase
    {
        [SerializeField] private Button _playButton;

        private Action _playClicked;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            }
        }

        public void SetActions(HomeViewActions actions)
        {
            _playClicked = actions.PlayClicked;
        }

        public override void Dispose()
        {
            _playClicked = null;

            base.Dispose();
        }

        private void OnPlayButtonClicked()
        {
            _playClicked?.Invoke();
        }
    }
}
