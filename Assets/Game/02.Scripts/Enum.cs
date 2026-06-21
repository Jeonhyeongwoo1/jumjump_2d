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
        Player_4 = 1004,
        Player_5 = 1005,
        Player_6 = 1006,
        Player_7 = 1007,
        Player_8 = 1008
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

    public enum LocalizationLanguageType
    {
        English,
        Korean
    }

    public enum GameSoundType
    {
        BgmGameLoop,
        UiButtonTap,
        UiCountdownTick,
        PlayerJump,
        LandingNormal,
        GoldCollect,
        PlayerMiss,
        PlayerDead
    }
}
