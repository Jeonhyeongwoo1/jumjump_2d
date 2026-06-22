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

    public struct ComboPlatformActivatedEvent
    {
        public PlatformController Platform { get; private set; }
        public Vector3 LandingPosition { get; private set; }
        public int ComboCount { get; private set; }
        public bool IsComboStarted { get; private set; }

        public ComboPlatformActivatedEvent(
            PlatformController platform,
            Vector3 landingPosition,
            int comboCount,
            bool isComboStarted)
        {
            Platform = platform;
            LandingPosition = landingPosition;
            ComboCount = comboCount;
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

}
