using System;

namespace JumJump.Data
{
    public static class ApiSchema
    {
        [Serializable]
        public sealed class LoginRequest
        {
            public string tossHash;

            public LoginRequest(string tossHash)
            {
                this.tossHash = tossHash;
            }
        }

        [Serializable]
        public sealed class LoginResponse
        {
            public string sessionToken;
            public UserData user;

            public string SessionToken => sessionToken;
            public UserData User => user;
        }

        [Serializable]
        public sealed class UserData
        {
            public string userId;
            public bool hasRemovedAds;
            public string nickname;
            public int highScore;
            public int gold;
            public int selectedPlayerSkinId;
            public long createdAtMillis;

            public string UserId => userId;
            public bool HasRemovedAds => hasRemovedAds;
            public string Nickname => nickname;
            public int HighScore => highScore;
            public int Gold => gold;
            public int SelectedPlayerSkinId => selectedPlayerSkinId;
            public long CreatedAtMillis => createdAtMillis;
        }

        [Serializable]
        public sealed class UserResponse
        {
            public UserData user;

            public UserData User => user;
        }

        [Serializable]
        public sealed class AdRemovalPurchaseRequest
        {
            public string orderId;
            public string productId;
            public long purchasedAtMillis;

            public AdRemovalPurchaseRequest(string orderId, string productId, long purchasedAtMillis)
            {
                this.orderId = orderId;
                this.productId = productId;
                this.purchasedAtMillis = purchasedAtMillis;
            }
        }

        [Serializable]
        public sealed class PlayerProgressRequest
        {
            public int highScore;
            public int gold;
            public int selectedPlayerSkinId;

            public PlayerProgressRequest(int highScore, int gold, int selectedPlayerSkinId)
            {
                this.highScore = highScore;
                this.gold = gold;
                this.selectedPlayerSkinId = selectedPlayerSkinId;
            }
        }

        [Serializable]
        public sealed class DailyPlayResponse
        {
            public DailyPlayData dailyPlay;

            public DailyPlayData DailyPlay => dailyPlay;
        }

        [Serializable]
        public sealed class DailyPlayData
        {
            public string todayDateKey;
            public int currentStreak;
            public int maxStreak;
            public int totalRecordedDays;
            public bool alreadyRecordedToday;
            public bool eligibleDay3Promotion;
            public bool eligibleDay7Promotion;
            public bool day3Claimed;
            public bool day7Claimed;

            public string TodayDateKey => todayDateKey;
            public int CurrentStreak => currentStreak;
            public int MaxStreak => maxStreak;
            public int TotalRecordedDays => totalRecordedDays;
            public bool AlreadyRecordedToday => alreadyRecordedToday;
            public bool EligibleDay3Promotion => eligibleDay3Promotion;
            public bool EligibleDay7Promotion => eligibleDay7Promotion;
            public bool Day3Claimed => day3Claimed;
            public bool Day7Claimed => day7Claimed;
        }

        [Serializable]
        public sealed class AttendancePromotionClaimRequest
        {
            public int milestoneDay;

            public AttendancePromotionClaimRequest(int milestoneDay)
            {
                this.milestoneDay = milestoneDay;
            }
        }

        [Serializable]
        public sealed class AttendancePromotionClaimResponse
        {
            public AttendancePromotionClaimData attendancePromotionClaim;

            public AttendancePromotionClaimData AttendancePromotionClaim => attendancePromotionClaim;
        }

        [Serializable]
        public sealed class AttendancePromotionClaimData
        {
            public int milestoneDay;
            public int currentStreak;
            public bool day3Claimed;
            public bool day7Claimed;

            public int MilestoneDay => milestoneDay;
            public int CurrentStreak => currentStreak;
            public bool Day3Claimed => day3Claimed;
            public bool Day7Claimed => day7Claimed;
        }
    }
}
