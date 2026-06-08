using JumJump.Util;
using UnityEngine;

namespace JumJump.Presenter
{
    public abstract class BaseSceneUI : MonoBehaviour
    {
        protected Canvas Canvas { get; private set; }

        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            Canvas.sortingOrder = GameConst.UI.SceneUISortingOrder;
        }
    }
}
