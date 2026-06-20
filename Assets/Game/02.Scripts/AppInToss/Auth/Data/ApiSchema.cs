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

            public string UserId => userId;
            public bool HasRemovedAds => hasRemovedAds;
            public string Nickname => nickname;
            public int HighScore => highScore;
            public int Gold => gold;
            public int SelectedPlayerSkinId => selectedPlayerSkinId;
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
    }
}
