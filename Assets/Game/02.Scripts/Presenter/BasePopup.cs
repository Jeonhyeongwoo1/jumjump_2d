using JumJump.Util;
using UnityEngine;

namespace JumJump.Presenter
{
    public abstract class BasePopup : MonoBehaviour
    {
        protected Canvas Canvas { get; private set; }

        public virtual void OnShown() { }
        public virtual void OnHidden() { }

        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            if (Canvas == null)
            {
                Debug.LogError($"[{GetType().Name}] Missing Canvas component on root GameObject.");
                return;
            }

            Canvas.sortingOrder = GameConst.UI.PopupSortingOrder;
        }
    }
}
