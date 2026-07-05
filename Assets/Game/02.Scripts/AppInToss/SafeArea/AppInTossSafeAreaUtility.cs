using JumJump.Bridge;
using UnityEngine;

namespace JumJump.Util
{
    public static class AppInTossSafeAreaUtility
    {
        private const string SafeAreaTargetName = "Pivot";

        public static RectTransform ResolveTarget(RectTransform root)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i) is RectTransform child && child.name == SafeAreaTargetName)
                {
                    return child;
                }
            }

            if (root.childCount == 1 && root.GetChild(0) is RectTransform onlyChild)
            {
                return onlyChild;
            }

            return root;
        }

        public static void Apply(RectTransform target, Canvas canvas, bool applyTopInset = true)
        {
            if (!AppInTossSafeAreaWebGL.IsReady())
            {
                return;
            }

            StretchToParent(target);

            var scaleFactor = Mathf.Max(GameConst.UI.MinimumCanvasScaleFactor, canvas.scaleFactor);
            var topInset = applyTopInset ? AppInTossSafeAreaWebGL.GetTop() : 0f;
            var offsetMin = new Vector2(
                AppInTossSafeAreaWebGL.GetLeft() / scaleFactor,
                AppInTossSafeAreaWebGL.GetBottom() / scaleFactor);
            var offsetMax = new Vector2(
                -AppInTossSafeAreaWebGL.GetRight() / scaleFactor,
                -topInset / scaleFactor);

            if (!IsApproximatelyEqual(target.offsetMin, offsetMin))
            {
                target.offsetMin = offsetMin;
            }

            if (!IsApproximatelyEqual(target.offsetMax, offsetMax))
            {
                target.offsetMax = offsetMax;
            }
        }

        private static void StretchToParent(RectTransform target)
        {
            if (!IsApproximatelyEqual(target.anchorMin, Vector2.zero))
            {
                target.anchorMin = Vector2.zero;
            }

            if (!IsApproximatelyEqual(target.anchorMax, Vector2.one))
            {
                target.anchorMax = Vector2.one;
            }
        }

        private static bool IsApproximatelyEqual(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }
}
