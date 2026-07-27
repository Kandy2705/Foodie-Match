using System;

namespace FoodieMatch.Core.Application.Shop
{
    public sealed class ShopPurchaseResult
    {
        private ShopPurchaseResult(
            bool isSuccess,
            ShopRewardApplyResult rewards,
            string errorMessage)
        {
            IsSuccess = isSuccess;
            Rewards = rewards;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public ShopRewardApplyResult Rewards { get; }

        public string ErrorMessage { get; }

        public static ShopPurchaseResult Succeeded(ShopRewardApplyResult rewards)
        {
            return new ShopPurchaseResult(
                true,
                rewards ?? throw new ArgumentNullException(nameof(rewards)),
                null);
        }

        public static ShopPurchaseResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException(
                    "A purchase failure needs an error message.",
                    nameof(errorMessage));
            }

            return new ShopPurchaseResult(false, null, errorMessage);
        }
    }
}
