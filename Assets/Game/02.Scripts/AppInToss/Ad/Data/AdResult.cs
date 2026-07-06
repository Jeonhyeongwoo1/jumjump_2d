namespace JumJump.Data
{
    public readonly struct AdResult
    {
        public bool IsSuccess { get; }
        public bool HasReward { get; }
        public string RewardType { get; }
        public int RewardAmount { get; }
        public string Error { get; }

        private AdResult(bool isSuccess, bool hasReward, string rewardType, int rewardAmount, string error)
        {
            IsSuccess = isSuccess;
            HasReward = hasReward;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
            Error = error;
        }

        public static AdResult Rewarded(string rewardType, int rewardAmount) =>
            new AdResult(true, true, rewardType, rewardAmount, string.Empty);

        public static AdResult Dismissed() =>
            new AdResult(true, false, string.Empty, 0, string.Empty);

        public static AdResult Failure(string error) =>
            new AdResult(false, false, string.Empty, 0, error);
    }
}
