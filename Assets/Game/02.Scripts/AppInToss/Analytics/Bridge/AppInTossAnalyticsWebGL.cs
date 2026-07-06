using System.Runtime.InteropServices;

namespace JumJump.Bridge
{
    public static class AppInTossAnalyticsWebGL
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AITAnalytics_EventLog(string payloadJson);
#endif

        public static void EventLog(string payloadJson)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AITAnalytics_EventLog(payloadJson);
#endif
        }
    }
}
