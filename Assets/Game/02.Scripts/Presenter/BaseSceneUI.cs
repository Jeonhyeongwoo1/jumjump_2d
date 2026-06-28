using JumJump.Util;
using UnityEngine;

namespace JumJump.Presenter
{
    public abstract class BaseSceneUI : MonoBehaviour
    {
        protected Canvas Canvas { get; private set; }

        private RectTransform _safeAreaTarget;
        private float _nextSafeAreaRefreshTime;
        private bool _isSafeAreaInitialized;
        private bool _isApplyingSafeArea;

        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            Canvas.sortingOrder = GameConst.UI.SceneUISortingOrder;
            _safeAreaTarget = AppInTossSafeAreaUtility.ResolveTarget((RectTransform)transform);
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
            if (_isApplyingSafeArea)
            {
                return;
            }

            _isApplyingSafeArea = true;
            try
            {
                AppInTossSafeAreaUtility.Apply(_safeAreaTarget, Canvas);
            }
            finally
            {
                _isApplyingSafeArea = false;
            }
        }
    }
}
