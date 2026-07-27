using System;

namespace FoodieMatch.Core.Application.Shop
{
    public sealed class ShopPaymentResult
    {
        private ShopPaymentResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static ShopPaymentResult Succeeded()
        {
            return new ShopPaymentResult(true, null);
        }

        public static ShopPaymentResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException(
                    "A payment failure needs an error message.",
                    nameof(errorMessage));
            }

            return new ShopPaymentResult(false, errorMessage);
        }
    }
}
