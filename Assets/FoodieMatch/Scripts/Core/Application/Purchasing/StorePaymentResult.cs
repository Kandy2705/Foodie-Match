using System;

namespace FoodieMatch.Core.Application.Purchasing
{
    public sealed class StorePaymentResult
    {
        private StorePaymentResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static StorePaymentResult Succeeded()
        {
            return new StorePaymentResult(true, null);
        }

        public static StorePaymentResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException(
                    "A payment failure needs an error message.",
                    nameof(errorMessage));
            }

            return new StorePaymentResult(false, errorMessage);
        }
    }
}
