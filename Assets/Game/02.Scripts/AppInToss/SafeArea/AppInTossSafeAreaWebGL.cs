using System.Runtime.InteropServices;

namespace JumJump.Bridge
{
    public static class AppInTossSafeAreaWebGL
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int  AITSafeArea_IsReady();
        [DllImport("__Internal")] private static extern float AITSafeArea_GetTop();
        [DllImport("__Internal")] private static extern float AITSafeArea_GetBottom();
        [DllImport("__Internal")] private static extern float AITSafeArea_GetLeft();
        [DllImport("__Internal")] private static extern float AITSafeArea_GetRight();
#endif

        public static bool IsReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITSafeArea_IsReady() == 1;
#else
            return false;
#endif
        }

        public static float GetTop()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITSafeArea_GetTop();
#else
            return 0f;
#endif
        }

        public static float GetBottom()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITSafeArea_GetBottom();
#else
            return 0f;
#endif
        }

        public static float GetLeft()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITSafeArea_GetLeft();
#else
            return 0f;
#endif
        }

        public static float GetRight()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITSafeArea_GetRight();
#else
            return 0f;
#endif
        }
    }
}
