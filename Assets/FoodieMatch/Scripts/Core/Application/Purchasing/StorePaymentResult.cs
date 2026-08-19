using System;

namespace FoodieMatch.Core.Application.Purchasing
{
    public sealed class StorePaymentResult
    {
        private StorePaymentResult(
            StorePaymentStatus status,
            string errorMessage)
        {
            Status = status;
            ErrorMessage = errorMessage;
        }

        public StorePaymentStatus Status { get; }

        public bool IsSuccess => Status == StorePaymentStatus.Succeeded;

        public bool IsCancelled => Status == StorePaymentStatus.Cancelled;

        public string ErrorMessage { get; }

        public static StorePaymentResult Succeeded()
        {
            return new StorePaymentResult(
                StorePaymentStatus.Succeeded,
                null);
        }

        public static StorePaymentResult Cancelled()
        {
            return new StorePaymentResult(
                StorePaymentStatus.Cancelled,
                null);
        }

        public static StorePaymentResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException(
                    "A payment failure needs an error message.",
                    nameof(errorMessage));
            }

            return new StorePaymentResult(
                StorePaymentStatus.Failed,
                errorMessage);
        }
    }
}
