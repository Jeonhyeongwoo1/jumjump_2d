using JumJump.Controller;
using UnityEngine;

namespace JumJump.Event
{
    public struct TapRequestedEvent
    {
    }

    public struct RestartRequestedEvent
    {
    }

    public struct ReviveRequestedEvent
    {
    }

    public struct ReviveOfferShownEvent
    {
    }

    public struct ReviveAdClickedEvent
    {
    }

    public struct GameStartedEvent
    {
    }

    public struct GameRevivedEvent
    {
    }

    public struct GameResetEvent
    {
    }

    public struct GameStateChangedEvent
    {
        public GameStateType State { get; private set; }

        public GameStateChangedEvent(GameStateType state)
        {
            State = state;
        }
    }

    public struct GameResourcesReadyEvent
    {
    }

    public struct PlayerSpawnedEvent
    {
        public Player Player { get; private set; }

        public PlayerSpawnedEvent(Player player)
        {
            Player = player;
        }
    }

    public struct PlayerJumpRequestedEvent
    {
    }

    public struct PlayerJumpStartedEvent
    {
    }

    public struct PlayerLandedEvent
    {
        public PlatformController Platform { get; private set; }
        public Vector3 LandingPosition { get; private set; }

        public PlayerLandedEvent(PlatformController platform, Vector3 landingPosition)
        {
            Platform = platform;
            LandingPosition = landingPosition;
        }
    }

    public struct PlayerMissedLandingEvent
    {
        public Vector2 KnockbackDirection { get; private set; }

        public PlayerMissedLandingEvent(Vector2 knockbackDirection)
        {
            KnockbackDirection = knockbackDirection;
        }
    }

    public struct PlayerDeadAnimationStartedEvent
    {
    }

    public struct PlatformShieldBlockedEvent
    {
        public PlatformController Platform { get; private set; }

        public PlatformShieldBlockedEvent(PlatformController platform)
        {
            Platform = platform;
        }
    }

    public struct RocketBoostPlatformsPassedEvent
    {
        public int PlatformCount { get; private set; }

        public RocketBoostPlatformsPassedEvent(int platformCount)
        {
            PlatformCount = platformCount;
        }
    }

    public struct GameOverResultViewRequestedEvent
    {
    }

    public struct RestartClickedEvent
    {
    }

    public struct ComboPlatformActivatedEvent
    {
        public PlatformController Platform { get; private set; }
        public Vector3 LandingPosition { get; private set; }
        public int ComboCount { get; private set; }
        public int ScoreDelta { get; private set; }
        public bool IsComboStarted { get; private set; }

        public ComboPlatformActivatedEvent(
            PlatformController platform,
            Vector3 landingPosition,
            int comboCount,
            int scoreDelta,
            bool isComboStarted)
        {
            Platform = platform;
            LandingPosition = landingPosition;
            ComboCount = comboCount;
            ScoreDelta = scoreDelta;
            IsComboStarted = isComboStarted;
        }
    }

    public struct ComboEndedEvent
    {
    }

    public struct SoundRequestedEvent
    {
        public GameSoundType Type { get; private set; }

        public SoundRequestedEvent(GameSoundType type)
        {
            Type = type;
        }
    }

    public struct HapticFeedbackRequestedEvent
    {
    }

    public struct PlatformsResetEvent
    {
        public PlatformController StartPlatform { get; private set; }

        public PlatformsResetEvent(PlatformController startPlatform)
        {
            StartPlatform = startPlatform;
        }
    }

    public struct ScoreChangedEvent
    {
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int ComboScore { get; private set; }
        public int ScoreDelta { get; private set; }
        public int BaseScoreDelta { get; private set; }
        public int ComboBonusDelta { get; private set; }
        public bool IsComboLandingScore { get; private set; }

        public ScoreChangedEvent(
            int score,
            int highScore,
            int comboScore,
            int scoreDelta,
            int baseScoreDelta,
            int comboBonusDelta,
            bool isComboLandingScore)
        {
            Score = score;
            HighScore = highScore;
            ComboScore = comboScore;
            ScoreDelta = scoreDelta;
            BaseScoreDelta = baseScoreDelta;
            ComboBonusDelta = comboBonusDelta;
            IsComboLandingScore = isComboLandingScore;
        }
    }

    public struct BestScoreReachedEvent
    {
        public int Score { get; private set; }
        public int PreviousHighScore { get; private set; }
        public Vector3 WorldPosition { get; private set; }

        public BestScoreReachedEvent(int score, int previousHighScore, Vector3 worldPosition)
        {
            Score = score;
            PreviousHighScore = previousHighScore;
            WorldPosition = worldPosition;
        }
    }

    public struct GoldChangedEvent
    {
        public int Gold { get; private set; }
        public int GoldDelta { get; private set; }
        public GoldChangeSourceType Source { get; private set; }

        public GoldChangedEvent(
            int gold,
            int goldDelta,
            GoldChangeSourceType source = GoldChangeSourceType.Gameplay)
        {
            Gold = gold;
            GoldDelta = goldDelta;
            Source = source;
        }
    }

    public struct PlayerProgressSavedEvent
    {
        public int HighScore { get; private set; }
        public int Gold { get; private set; }
        public int SelectedPlayerSkinId { get; private set; }

        public PlayerProgressSavedEvent(int highScore, int gold, int selectedPlayerSkinId)
        {
            HighScore = highScore;
            Gold = gold;
            SelectedPlayerSkinId = selectedPlayerSkinId;
        }
    }

    public struct GameOverEvent
    {
        public int Score { get; private set; }
        public int HighScore { get; private set; }

        public GameOverEvent(int score, int highScore)
        {
            Score = score;
            HighScore = highScore;
        }
    }

    public struct AuthLoginCompletedEvent
    {
        public string UserId { get; private set; }
        public string Nickname { get; private set; }
        public bool HasRemovedAds { get; private set; }

        public AuthLoginCompletedEvent(string userId, string nickname, bool hasRemovedAds)
        {
            UserId = userId;
            Nickname = nickname;
            HasRemovedAds = hasRemovedAds;
        }
    }

    public struct AuthLoginFailedEvent
    {
        public string Error { get; private set; }

        public AuthLoginFailedEvent(string error)
        {
            Error = error;
        }
    }

    public struct AuthUserRefreshedEvent
    {
        public string UserId { get; private set; }
        public string Nickname { get; private set; }
        public bool HasRemovedAds { get; private set; }

        public AuthUserRefreshedEvent(string userId, string nickname, bool hasRemovedAds)
        {
            UserId = userId;
            Nickname = nickname;
            HasRemovedAds = hasRemovedAds;
        }
    }

    public struct AdRewardedEvent
    {
        public string AdGroupId { get; private set; }
        public string RewardType { get; private set; }
        public int RewardAmount { get; private set; }

        public AdRewardedEvent(string adGroupId, string rewardType, int rewardAmount)
        {
            AdGroupId = adGroupId;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
        }
    }

    public struct AdEventLoggedEvent
    {
        public string AdGroupId { get; private set; }
        public string EventType { get; private set; }
        public int Score { get; private set; }
        public string Error { get; private set; }
        public bool HasReward { get; private set; }
        public string RewardType { get; private set; }
        public int RewardAmount { get; private set; }

        public AdEventLoggedEvent(
            string adGroupId,
            string eventType,
            int score,
            string error = "",
            bool hasReward = false,
            string rewardType = "",
            int rewardAmount = 0)
        {
            AdGroupId = adGroupId;
            EventType = eventType;
            Score = score;
            Error = error;
            HasReward = hasReward;
            RewardType = rewardType;
            RewardAmount = rewardAmount;
        }
    }

    public struct IAPEventLoggedEvent
    {
        public string ProductId { get; private set; }
        public string EventType { get; private set; }
        public string OrderId { get; private set; }
        public string Error { get; private set; }

        public IAPEventLoggedEvent(
            string productId,
            string eventType,
            string orderId = "",
            string error = "")
        {
            ProductId = productId;
            EventType = eventType;
            OrderId = orderId;
            Error = error;
        }
    }

    public struct PromotionEventLoggedEvent
    {
        public string PromotionCode { get; private set; }
        public string CampaignType { get; private set; }
        public string EventType { get; private set; }
        public int Amount { get; private set; }
        public string RewardKey { get; private set; }
        public string ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; }

        public PromotionEventLoggedEvent(
            string promotionCode,
            string campaignType,
            string eventType,
            int amount,
            string rewardKey = "",
            string errorCode = "",
            string errorMessage = "")
        {
            PromotionCode = promotionCode;
            CampaignType = campaignType;
            EventType = eventType;
            Amount = amount;
            RewardKey = rewardKey;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}
