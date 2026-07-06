using System;

namespace JumJump.Data
{
    public readonly struct PromotionRewardResult
    {
        public bool IsSuccess { get; }
        public bool IsUnsupported { get; }
        public string RewardKey { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        private PromotionRewardResult(
            bool isSuccess,
            bool isUnsupported,
            string rewardKey,
            string errorCode,
            string errorMessage)
        {
            IsSuccess = isSuccess;
            IsUnsupported = isUnsupported;
            RewardKey = rewardKey;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static PromotionRewardResult Success(string rewardKey) =>
            new PromotionRewardResult(true, false, rewardKey, string.Empty, string.Empty);

        public static PromotionRewardResult Unsupported() =>
            new PromotionRewardResult(false, true, string.Empty, "unsupported_toss_app_version", string.Empty);

        public static PromotionRewardResult Failure(string errorCode, string errorMessage = "") =>
            new PromotionRewardResult(false, false, string.Empty, errorCode, errorMessage);
    }

    [Serializable]
    public sealed class PromotionRewardResponse
    {
        public bool success;
        public bool unsupported;
        public string key;
        public string errorCode;
        public string message;
    }
}
