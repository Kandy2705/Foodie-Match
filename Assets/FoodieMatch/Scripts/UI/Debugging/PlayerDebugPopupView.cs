using System;
using FoodieMatch.Core.Application.Player;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Debugging
{
    public sealed class PlayerDebugPopupView : PopupBase
    {
        [SerializeField] private TMP_InputField _currentLevelInput;
        [SerializeField] private TMP_InputField _coinBalanceInput;
        [SerializeField] private TMP_InputField _heartCountInput;
        [SerializeField] private TMP_InputField _plateBoosterCountInput;
        [SerializeField] private TMP_InputField _storageBoosterCountInput;
        [SerializeField] private TMP_InputField _swapBoosterCountInput;
        [SerializeField] private TMP_InputField _fridgeBoosterCountInput;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action<PlayerProfileDebugUpdate> _applyClicked;
        private int _maxHeartCount;

        private void Awake()
        {
            _applyButton.onClick.AddListener(OnApplyButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnDestroy()
        {
            _applyButton.onClick.RemoveListener(OnApplyButtonClicked);
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        public void SetActions(PlayerDebugPopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _applyClicked = actions.ApplyClicked;
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

        public void SetValues(PlayerProfileDebugUpdate values, int maxHeartCount)
        {
            _maxHeartCount = maxHeartCount;
            _currentLevelInput.SetTextWithoutNotify(values.CurrentLevelNumber.ToString());
            _coinBalanceInput.SetTextWithoutNotify(values.CoinBalance.ToString());
            _heartCountInput.SetTextWithoutNotify(values.HeartCount.ToString());
            _plateBoosterCountInput.SetTextWithoutNotify(values.PlateBoosterCount.ToString());
            _storageBoosterCountInput.SetTextWithoutNotify(values.StorageBoosterCount.ToString());
            _swapBoosterCountInput.SetTextWithoutNotify(values.SwapBoosterCount.ToString());
            _fridgeBoosterCountInput.SetTextWithoutNotify(values.FridgeBoosterCount.ToString());
            ShowStatus(string.Empty);
        }

        public void ShowStatus(string message)
        {
            _statusText.text = message;
        }

        public override void Dispose()
        {
            _closeClicked = null;
            _applyClicked = null;

            base.Dispose();
        }

        private void OnApplyButtonClicked()
        {
            if (!TryGetValues(out PlayerProfileDebugUpdate values))
            {
                return;
            }

            _applyClicked(values);
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

        private bool TryGetValues(out PlayerProfileDebugUpdate values)
        {
            values = default;

            if (!int.TryParse(_currentLevelInput.text, out int currentLevelNumber) ||
                currentLevelNumber < 1)
            {
                ShowStatus("Level must be a whole number of at least 1.");
                return false;
            }

            if (!long.TryParse(_coinBalanceInput.text, out long coinBalance) ||
                coinBalance < 0)
            {
                ShowStatus("Coin balance must be a non-negative whole number.");
                return false;
            }

            if (!TryGetHeartCount(out int heartCount) ||
                !TryGetBoosterCount(_plateBoosterCountInput, "Plate", out int plateBoosterCount) ||
                !TryGetBoosterCount(_storageBoosterCountInput, "Storage", out int storageBoosterCount) ||
                !TryGetBoosterCount(_swapBoosterCountInput, "Swap", out int swapBoosterCount) ||
                !TryGetBoosterCount(_fridgeBoosterCountInput, "Fridge", out int fridgeBoosterCount))
            {
                return false;
            }

            values = new PlayerProfileDebugUpdate(
                currentLevelNumber,
                coinBalance,
                heartCount,
                plateBoosterCount,
                storageBoosterCount,
                swapBoosterCount,
                fridgeBoosterCount);

            return true;
        }

        private bool TryGetHeartCount(out int heartCount)
        {
            if (int.TryParse(_heartCountInput.text, out heartCount) &&
                heartCount >= 0 &&
                heartCount <= _maxHeartCount)
            {
                return true;
            }

            ShowStatus($"Heart count must be between 0 and {_maxHeartCount}.");
            return false;
        }

        private bool TryGetBoosterCount(TMP_InputField input, string boosterName, out int boosterCount)
        {
            if (int.TryParse(input.text, out boosterCount) &&
                boosterCount >= 0)
            {
                return true;
            }

            ShowStatus($"{boosterName} count must be a non-negative whole number.");
            return false;
        }
    }
}
