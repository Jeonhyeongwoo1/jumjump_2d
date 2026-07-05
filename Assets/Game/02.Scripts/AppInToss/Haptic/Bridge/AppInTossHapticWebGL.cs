using System.Runtime.InteropServices;

namespace JumJump.Bridge
{
    public static class AppInTossHapticWebGL
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AITHaptic_VibrateLight();
#endif

        public static void VibrateLight()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AITHaptic_VibrateLight();
#endif
        }
    }
}
