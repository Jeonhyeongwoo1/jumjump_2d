using System.Runtime.InteropServices;

namespace JumJump.Bridge
{
    public static class AppInTossAdWebGL
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AITAd_Load(string adGroupId, int wave);
        [DllImport("__Internal")] private static extern int AITAd_IsLoadCompleted();
        [DllImport("__Internal")] private static extern string AITAd_GetLoadError();
        [DllImport("__Internal")] private static extern void AITAd_Show(string adGroupId, int wave);
        [DllImport("__Internal")] private static extern int AITAd_IsShowCompleted();
        [DllImport("__Internal")] private static extern string AITAd_GetShowError();
        [DllImport("__Internal")] private static extern int AITAd_HasReward();
        [DllImport("__Internal")] private static extern string AITAd_GetRewardType();
        [DllImport("__Internal")] private static extern int AITAd_GetRewardAmount();
        [DllImport("__Internal")] private static extern void AITAd_ResumeAudio();
#endif

        public static void Load(string adGroupId, int wave)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AITAd_Load(adGroupId, wave);
#endif
        }

        public static bool IsLoadCompleted()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAd_IsLoadCompleted() == 1;
#else
            return false;
#endif
        }

        public static string GetLoadError()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAd_GetLoadError();
#else
            return string.Empty;
#endif
        }

        public static void Show(string adGroupId, int wave)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AITAd_Show(adGroupId, wave);
#endif
        }

        public static bool IsShowCompleted()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAd_IsShowCompleted() == 1;
#else
            return false;
#endif
        }

        public static string GetShowError()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAd_GetShowError();
#else
            return string.Empty;
#endif
        }

        public static bool HasReward()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAd_HasReward() == 1;
#else
            return false;
#endif
        }

        public static string GetRewardType()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAd_GetRewardType();
#else
            return string.Empty;
#endif
        }

        public static int GetRewardAmount()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITAd_GetRewardAmount();
#else
            return 0;
#endif
        }

        public static void ResumeAudio()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AITAd_ResumeAudio();
#endif
        }
    }
}
