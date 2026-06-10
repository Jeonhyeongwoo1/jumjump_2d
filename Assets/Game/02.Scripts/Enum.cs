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

    public enum PlatformStateType
    {
        Moving,
        Resolved,
        ShieldBlockedDissolving,
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
}
