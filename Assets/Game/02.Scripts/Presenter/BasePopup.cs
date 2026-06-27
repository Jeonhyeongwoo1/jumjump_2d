using JumJump.Util;
using UnityEngine;

namespace JumJump.Presenter
{
    public abstract class BasePopup : MonoBehaviour
    {
        protected Canvas Canvas { get; private set; }

        private float _nextSafeAreaRefreshTime;
        private bool _isSafeAreaInitialized;

        public virtual void OnShown() { }
        public virtual void OnHidden() { }

        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            Canvas.sortingOrder = GameConst.UI.PopupSortingOrder;
            _isSafeAreaInitialized = true;
            ApplySafeArea();
        }

        protected virtual void Start()
        {
            ApplySafeArea();
        }

        protected virtual void OnEnable()
        {
            if (!_isSafeAreaInitialized)
            {
                return;
            }

            ApplySafeArea();
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
            if (!_isSafeAreaInitialized)
            {
                return;
            }

            ApplySafeArea();
        }

        protected virtual void LateUpdate()
        {
            if (!_isSafeAreaInitialized || Time.unscaledTime < _nextSafeAreaRefreshTime)
            {
                return;
            }

            _nextSafeAreaRefreshTime = Time.unscaledTime + GameConst.UI.SafeAreaRefreshInterval;
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            AppInTossSafeAreaUtility.Apply((RectTransform)transform, Canvas);
        }
    }
}
