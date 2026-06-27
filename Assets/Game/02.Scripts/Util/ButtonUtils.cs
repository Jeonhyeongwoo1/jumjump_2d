using System;
using UnityEngine.UI;

namespace JumJump.Util
{
    public static class ButtonUtils
    {
        public static void SetListener(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action.Invoke);
        }
    }
}
