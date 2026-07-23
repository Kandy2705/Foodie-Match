using System;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Advertising
{
    public sealed class FakeRewardedAdPopupView : PopupBase
    {
        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _cancelButton;

        private Action _completed;
        private Action _cancelled;
        private bool _isSelectionEnabled;

        private void Awake()
        {
            if (_completeButton != null)
            {
                _completeButton.onClick.AddListener(OnCompleteButtonClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_completeButton != null)
            {
                _completeButton.onClick.RemoveListener(OnCompleteButtonClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }
        }

        public override void Show()
        {
            base.Show();
            SetSelectionEnabled(true);
        }

        public void SetActions(FakeRewardedAdPopupViewActions actions)
        {
            _completed = actions.Completed;
            _cancelled = actions.Cancelled;
        }

        public override void Dispose()
        {
            _completed = null;
            _cancelled = null;
            _isSelectionEnabled = false;

            base.Dispose();
        }

        private void OnCompleteButtonClicked()
        {
            InvokeSelection(_completed);
        }

        private void OnCancelButtonClicked()
        {
            InvokeSelection(_cancelled);
        }

        private void InvokeSelection(Action selection)
        {
            if (!_isSelectionEnabled || selection == null)
            {
                return;
            }

            SetSelectionEnabled(false);
            selection.Invoke();
        }

        private void SetSelectionEnabled(bool isEnabled)
        {
            _isSelectionEnabled = isEnabled;

            if (_completeButton != null)
            {
                _completeButton.interactable = isEnabled;
            }

            if (_cancelButton != null)
            {
                _cancelButton.interactable = isEnabled;
            }
        }
    }
}
