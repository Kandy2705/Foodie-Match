using FoodieMatch.Core.Domain.RequiredPackage;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageView : MonoBehaviour
    {
        private const int MinRequiredAmount = 1;
        private const int MaxRequiredAmount = 3;

        [SerializeField] private RequiredPackageAmountView _amount1View;
        [SerializeField] private RequiredPackageAmountView _amount2View;
        [SerializeField] private RequiredPackageAmountView _amount3View;

        private Sprite _sprite;

        public int FoodTokenId { get; private set; }
        public int RequiredAmount { get; private set; }
        public int FilledAmount { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsComplete => !IsEmpty && FilledAmount >= RequiredAmount;

        public void Setup(int foodTokenId, int requiredAmount, Sprite sprite)
        {
            if (foodTokenId <= 0)
            {
                Debug.LogWarning($"Required package food token id must be greater than 0: {foodTokenId}.", this);
                Clear();
                return;
            }

            if (requiredAmount < MinRequiredAmount || requiredAmount > MaxRequiredAmount)
            {
                Debug.LogWarning($"Required package amount must be from {MinRequiredAmount} to {MaxRequiredAmount}: {requiredAmount}.", this);
                Clear();
                return;
            }

            FoodTokenId = foodTokenId;
            RequiredAmount = requiredAmount;
            FilledAmount = 0;
            _sprite = sprite;

            RefreshActiveView();
        }

        public void SetFilledAmount(int filledAmount)
        {
            FilledAmount = Mathf.Clamp(filledAmount, 0, RequiredAmount);
            RefreshActiveView();
        }

        public RequiredPackageState GetState()
        {
            return new RequiredPackageState(FoodTokenId, RequiredAmount, FilledAmount);
        }

        public void Clear()
        {
            FoodTokenId = 0;
            RequiredAmount = 0;
            FilledAmount = 0;
            _sprite = null;

            HideAllViews();
        }

        private void RefreshActiveView()
        {
            HideAllViews();

            RequiredPackageAmountView activeView = GetView(RequiredAmount);

            if (activeView == null)
            {
                Debug.LogWarning($"Required package view for amount {RequiredAmount} is missing.", this);
                return;
            }

            if (activeView.SlotCount != RequiredAmount)
            {
                Debug.LogWarning($"Required package view for amount {RequiredAmount} must have {RequiredAmount} slots.", this);
            }

            activeView.Show(_sprite, FilledAmount);
        }

        private RequiredPackageAmountView GetView(int requiredAmount)
        {
            return requiredAmount switch
            {
                1 => _amount1View,
                2 => _amount2View,
                3 => _amount3View,
                _ => null
            };
        }

        private void HideAllViews()
        {
            _amount1View?.Hide();
            _amount2View?.Hide();
            _amount3View?.Hide();
        }
    }
}
