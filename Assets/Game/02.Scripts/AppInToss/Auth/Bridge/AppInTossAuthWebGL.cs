using System.Runtime.InteropServices;

namespace JumJump.Bridge
{
    public static class AppInTossAuthWebGL
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void AITAuth_StartLogin(string loginUrl);

        [DllImport("__Internal")]
        private static extern int AITAuth_IsLoginCompleted();

        [DllImport("__Internal")]
        private static extern string AITAuth_GetLoginResultJson();

        [DllImport("__Internal")]
        private static extern string AITAuth_GetLoginError();
#endif

        public static void StartLogin(string loginUrl)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AITAuth_StartLogin(loginUrl);
#endif
        }

        public static bool IsLoginCompleted()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAuth_IsLoginCompleted() == 1;
#else
            return false;
#endif
        }

        public static string GetLoginResultJson()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAuth_GetLoginResultJson();
#else
            return string.Empty;
#endif
        }

        public static string GetLoginError()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAuth_GetLoginError();
#else
            return string.Empty;
#endif
        }
    }
}
