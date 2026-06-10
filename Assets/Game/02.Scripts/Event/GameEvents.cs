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

    public struct GameStartedEvent
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

        public ScoreChangedEvent(int score, int highScore)
        {
            Score = score;
            HighScore = highScore;
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
