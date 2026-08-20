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
        [SerializeField] private TMP_InputField _goldPassSpoonCountInput;
        [SerializeField] private Toggle _seasonPassPurchasedToggle;
        [SerializeField] private Toggle _adsRemovedToggle;
        [SerializeField] private Toggle _postLevelAdsToggle;
        [SerializeField] private Toggle _useLevelPlayAdsToggle;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetGoldPassClaimHistoryButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;
        private Action<DebugMenuValues> _applyClicked;
        private Action _resetGoldPassClaimHistoryClicked;
        private int _maxHeartCount;

        private void Awake()
        {
            _applyButton.onClick.AddListener(OnApplyButtonClicked);
            _resetGoldPassClaimHistoryButton.onClick.AddListener(
                OnResetGoldPassClaimHistoryButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnDestroy()
        {
            _applyButton.onClick.RemoveListener(OnApplyButtonClicked);
            _resetGoldPassClaimHistoryButton.onClick.RemoveListener(
                OnResetGoldPassClaimHistoryButtonClicked);
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        public void SetActions(PlayerDebugPopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
            _applyClicked = actions.ApplyClicked;
            _resetGoldPassClaimHistoryClicked =
                actions.ResetGoldPassClaimHistoryClicked;
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

        public void SetValues(DebugMenuValues values, int maxHeartCount)
        {
            PlayerProfileDebugUpdate playerProfile = values.PlayerProfile;
            _maxHeartCount = maxHeartCount;
            _currentLevelInput.SetTextWithoutNotify(playerProfile.CurrentLevelNumber.ToString());
            _coinBalanceInput.SetTextWithoutNotify(playerProfile.CoinBalance.ToString());
            _heartCountInput.SetTextWithoutNotify(playerProfile.HeartCount.ToString());
            _plateBoosterCountInput.SetTextWithoutNotify(playerProfile.PlateBoosterCount.ToString());
            _storageBoosterCountInput.SetTextWithoutNotify(playerProfile.StorageBoosterCount.ToString());
            _swapBoosterCountInput.SetTextWithoutNotify(playerProfile.SwapBoosterCount.ToString());
            _fridgeBoosterCountInput.SetTextWithoutNotify(playerProfile.FridgeBoosterCount.ToString());
            _goldPassSpoonCountInput.SetTextWithoutNotify(
                values.GoldPassSpoonCount.ToString());
            _seasonPassPurchasedToggle.SetIsOnWithoutNotify(
                values.IsSeasonPassPurchased);
            _adsRemovedToggle.SetIsOnWithoutNotify(
                values.AdsRemoved);
            _postLevelAdsToggle.SetIsOnWithoutNotify(
                values.PostLevelAdsEnabled);
            _useLevelPlayAdsToggle.SetIsOnWithoutNotify(
                values.UseLevelPlayAds);
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
            _resetGoldPassClaimHistoryClicked = null;

            base.Dispose();
        }

        private void OnApplyButtonClicked()
        {
            if (!TryGetValues(out DebugMenuValues values))
            {
                return;
            }

            _applyClicked(values);
        }

        private void OnCloseButtonClicked()
        {
            _closeClicked();
        }

        private void OnResetGoldPassClaimHistoryButtonClicked()
        {
            _resetGoldPassClaimHistoryClicked();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }

        private bool TryGetValues(out DebugMenuValues values)
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
                !TryGetBoosterCount(_fridgeBoosterCountInput, "Fridge", out int fridgeBoosterCount) ||
                !TryGetGoldPassSpoonCount(out int goldPassSpoonCount))
            {
                return false;
            }

            PlayerProfileDebugUpdate playerProfile = new(
                currentLevelNumber,
                coinBalance,
                heartCount,
                plateBoosterCount,
                storageBoosterCount,
                swapBoosterCount,
                fridgeBoosterCount,
                _adsRemovedToggle.isOn);
            values = new DebugMenuValues(
                playerProfile,
                goldPassSpoonCount,
                _seasonPassPurchasedToggle.isOn,
                _adsRemovedToggle.isOn,
                _postLevelAdsToggle.isOn,
                _useLevelPlayAdsToggle.isOn);

            return true;
        }

        private bool TryGetGoldPassSpoonCount(out int spoonCount)
        {
            if (int.TryParse(_goldPassSpoonCountInput.text, out spoonCount) &&
                spoonCount >= 0)
            {
                return true;
            }

            ShowStatus(
                "Gold Pass spoon count must be a non-negative whole number.");
            return false;
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
