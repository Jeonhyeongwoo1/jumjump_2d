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
    }
}
