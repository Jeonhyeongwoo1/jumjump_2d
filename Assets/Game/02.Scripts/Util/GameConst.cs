namespace JumJump.Util
{
    public static class GameConst
    {
        public static class UI
        {
            public const int SceneUISortingOrder = 100;
            public const int PopupSortingOrder = 1000;
            public const int StartCountdownSeconds = 3;
            public const float StartCountdownScaleFrom = 0.5f;
            public const float StartCountdownScalePop = 1.45f;
            public const float StartCountdownScaleSettle = 1f;
            public const float StartCountdownScaleOut = 0.8f;
            public const float StartCountdownPopInDuration = 0.12f;
            public const float StartCountdownSettleDuration = 0.22f;
            public const float StartCountdownFadeOutStart = 0.8f;
            public const int GameOverCountdownSeconds = 5;
        }

        public static class Platform
        {
            public const float MinimumColliderDimension = 0.01f;
            public const float MinimumWidthScale = 0.01f;
            public const float MoveTargetEpsilon = 0.001f;
            public const float CleanupYMargin = 0.05f;
            public const float ShieldBlockedFadeDuration = 0.45f;
            public const float ShieldBlockedRetreatDistance = 0.7f;
            public const float ShieldBlockedRespawnDelay = 1f;
            public const float RocketPathPlayerLeadEntryRatio = 0.45f;
            public const float RocketPathFinalArrivalLeadEntryRatio = 0.25f;
            public const float RocketPathEntryDurationScale = 1.35f;
        }

        public static class Environment
        {
            public const int GradientTextureWidth = 32;
            public const int GradientTextureHeight = 32;
        }

        public static class DynamicFont
        {
            public const float AnimationDuration = 1f;
            public const float RiseHeight = 0.8f;
            public const float SpawnOffsetX = 0.5f;
            public const float SpawnOffsetY = 0.3f;
        }

        public static class Player
        {
            public const float ShieldBreakAnimationDuration = 0.52f;
        }
    }
}
