using JumJump.Controller;

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

    public struct PlayerJumpRequestedEvent
    {
    }

    public struct PlayerJumpStartedEvent
    {
    }

    public struct PlayerLandedEvent
    {
        public PlatformController Platform { get; private set; }

        public PlayerLandedEvent(PlatformController platform)
        {
            Platform = platform;
        }
    }

    public struct PlayerMissedLandingEvent
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
