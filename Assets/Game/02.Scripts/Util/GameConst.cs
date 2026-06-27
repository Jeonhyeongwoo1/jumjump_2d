namespace JumJump.Util
{
    public static class GameConst
    {
        public static class UI
        {
            public const int SceneUISortingOrder = 100;
            public const int LoadingSortingOrder = 2000;
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
            public const float GameOverResultViewRestartDelay = 2f;
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

        public static class Score
        {
            public const float RocketBoostIncrementInterval = 0.06f;
            public const int ScoreBoardMinimumHighScore = 50;
            public const float ScoreBoardViewportX = 0.25f;
            public const float ScoreBoardTargetOffsetY = 0.85f;
            public const int ScoreBoardSortingOrder = 20;
        }

        public static class Camera
        {
            public const float GameOverOverviewBottomViewportY = 0f;
            public const float GameOverOverviewTopViewportY = 0.9f;
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
            public const float ComboScale = 1.18f;
            public const float OutlineWidth = 0.16f;
            public const byte NormalScoreColorR = 255;
            public const byte NormalScoreColorG = 255;
            public const byte NormalScoreColorB = 255;
            public const byte OutlineColorR = 26;
            public const byte OutlineColorG = 18;
            public const byte OutlineColorB = 8;
            public const byte ComboGradientTopLeftR = 255;
            public const byte ComboGradientTopLeftG = 255;
            public const byte ComboGradientTopLeftB = 255;
            public const byte ComboGradientTopRightR = 255;
            public const byte ComboGradientTopRightG = 255;
            public const byte ComboGradientTopRightB = 255;
            public const byte ComboGradientBottomLeftR = 255;
            public const byte ComboGradientBottomLeftG = 236;
            public const byte ComboGradientBottomLeftB = 61;
            public const byte ComboGradientBottomRightR = 255;
            public const byte ComboGradientBottomRightG = 236;
            public const byte ComboGradientBottomRightB = 61;
        }

        public static class Player
        {
            public const float ShieldBreakAnimationDuration = 0.52f;
        }
    }
}
