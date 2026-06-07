namespace JumJump.Util
{
    public static class GameConst
    {
        public static class UI
        {
            public const int SceneUISortingOrder = 100;
            public const int PopupSortingOrder = 1000;
            public const int GameOverCountdownSeconds = 5;
        }

        public static class Platform
        {
            public const float MinimumColliderDimension = 0.01f;
            public const float MoveTargetEpsilon = 0.001f;
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
    }
}
