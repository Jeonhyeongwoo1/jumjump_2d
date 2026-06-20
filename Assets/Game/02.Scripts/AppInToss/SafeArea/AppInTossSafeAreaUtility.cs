using JumJump.Bridge;
using UnityEngine;

namespace JumJump.Util
{
    public static class AppInTossSafeAreaUtility
    {
        public static void Apply(RectTransform target, Canvas canvas)
        {
            if (!AppInTossSafeAreaWebGL.IsReady())
            {
                return;
            }

            var scaleFactor = Mathf.Max(GameConst.UI.MinimumCanvasScaleFactor, canvas.scaleFactor);
            var offsetMin = new Vector2(
                AppInTossSafeAreaWebGL.GetLeft() / scaleFactor,
                AppInTossSafeAreaWebGL.GetBottom() / scaleFactor);
            var offsetMax = new Vector2(
                -AppInTossSafeAreaWebGL.GetRight() / scaleFactor,
                -AppInTossSafeAreaWebGL.GetTop() / scaleFactor);

            if (IsApproximatelyEqual(target.offsetMin, offsetMin) &&
                IsApproximatelyEqual(target.offsetMax, offsetMax))
            {
                return;
            }

            target.offsetMin = offsetMin;
            target.offsetMax = offsetMax;
        }

        private static bool IsApproximatelyEqual(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }
}
