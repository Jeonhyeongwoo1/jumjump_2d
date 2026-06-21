namespace JumJump
{
    public enum GameStateType
    {
        Ready,
        Playing,
        GameOver
    }

    public enum PlayerStateType
    {
        Idle,
        Jump,
        RocketBoost,
        Knockback
    }

    public enum PlayerSkinType
    {
        Player_1 = 1001,
        Player_2 = 1002,
        Player_3 = 1003,
        Player_4 = 1004
    }

    public enum PlatformStateType
    {
        Moving,
        Resolved,
        ShieldBlockedDissolving,
        Archived,
        Released
    }

    public enum PlatformGimmickType
    {
        Normal,
        Small,
        Fast,
        Slow,
        SmallAndFast,
        Ghost,
        Double,
        Reveal,
        Shield,
        Rocket
    }

    public enum GameLogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Off
    }
}
