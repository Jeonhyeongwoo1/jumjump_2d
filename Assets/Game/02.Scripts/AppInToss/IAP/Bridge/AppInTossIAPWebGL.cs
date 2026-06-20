using System.Runtime.InteropServices;

namespace JumJump.Bridge
{
    public static class AppInTossIAPWebGL
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AITIA_Purchase(string productId);
        [DllImport("__Internal")] private static extern int AITIA_IsPurchaseCompleted();
        [DllImport("__Internal")] private static extern string AITIA_GetPurchaseResultJson();
        [DllImport("__Internal")] private static extern string AITIA_GetPurchaseError();
#endif

        public static void Purchase(string productId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AITIA_Purchase(productId);
#endif
        }

        public static bool IsPurchaseCompleted()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITIA_IsPurchaseCompleted() == 1;
#else
            return false;
#endif
        }

        public static string GetPurchaseResultJson()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITIA_GetPurchaseResultJson();
#else
            return string.Empty;
#endif
        }

        public static string GetPurchaseError()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return AITIA_GetPurchaseError();
#else
            return string.Empty;
#endif
        }
    }
}
