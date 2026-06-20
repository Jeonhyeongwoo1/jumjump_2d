using System;

namespace JumJump.Data
{
    public readonly struct IAPResult
    {
        public bool IsSuccess { get; }
        public bool IsCancelled { get; }
        public string OrderId { get; }
        public string ProductId { get; }
        public long PurchasedAtMillis { get; }
        public string Error { get; }

        private IAPResult(bool isSuccess, bool isCancelled, string orderId, string productId, long purchasedAtMillis, string error)
        {
            IsSuccess = isSuccess;
            IsCancelled = isCancelled;
            OrderId = orderId;
            ProductId = productId;
            PurchasedAtMillis = purchasedAtMillis;
            Error = error;
        }

        public static IAPResult Success(string orderId, string productId, long purchasedAtMillis) =>
            new IAPResult(true, false, orderId, productId, purchasedAtMillis, string.Empty);

        public static IAPResult Cancelled() =>
            new IAPResult(false, true, string.Empty, string.Empty, 0, "cancelled");

        public static IAPResult Failure(string error) =>
            new IAPResult(false, false, string.Empty, string.Empty, 0, error);
    }

    [Serializable]
    public class IAPPurchaseResponse
    {
        public string orderId;
        public string productId;
        public long purchasedAtMillis;
    }
}
