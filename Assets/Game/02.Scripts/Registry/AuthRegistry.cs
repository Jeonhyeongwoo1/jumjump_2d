namespace JumJump.Registry
{
    public sealed class AuthRegistry
    {
        public string SessionToken { get; private set; }
        public string UserId { get; private set; }
        public string Nickname { get; private set; }
        public bool HasRemovedAds { get; private set; }
        public long CreatedAtMillis { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(SessionToken) && !string.IsNullOrEmpty(UserId);

        public void SetSessionToken(string sessionToken)
        {
            SessionToken = sessionToken;
        }

        public void SetUserData(string userId, bool hasRemovedAds, string nickname, long createdAtMillis)
        {
            UserId = userId;
            HasRemovedAds = hasRemovedAds;
            Nickname = nickname;
            CreatedAtMillis = createdAtMillis;
        }
    }
}
