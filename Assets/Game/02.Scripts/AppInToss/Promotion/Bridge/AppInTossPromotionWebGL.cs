using System.Runtime.InteropServices;

namespace JumJump.Bridge
{
    public static class AppInTossPromotionWebGL
    {
        [DllImport("__Internal")] private static extern void AITPromotion_Grant(string promotionCode, int amount);
        [DllImport("__Internal")] private static extern int AITPromotion_IsGrantCompleted();
        [DllImport("__Internal")] private static extern string AITPromotion_GetGrantResultJson();
        [DllImport("__Internal")] private static extern string AITPromotion_GetGrantError();

        public static void Grant(string promotionCode, int amount)
        {
            AITPromotion_Grant(promotionCode, amount);
        }

        public static bool IsGrantCompleted()
        {
            return AITPromotion_IsGrantCompleted() == 1;
        }

        public static string GetGrantResultJson()
        {
            return AITPromotion_GetGrantResultJson();
        }

        public static string GetGrantError()
        {
            return AITPromotion_GetGrantError();
        }
    }
}
