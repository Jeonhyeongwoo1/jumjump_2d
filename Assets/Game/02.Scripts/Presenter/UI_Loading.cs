using JumJump.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumJump.Presenter
{
    public sealed class UI_Loading : BaseSceneUI
    {
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private string _progressTextFormat = "Loading...{0}%";

        protected override void Awake()
        {
            base.Awake();
            Canvas.sortingOrder = GameConst.UI.LoadingSortingOrder;
            SetProgress(0f);
        }

        public void BindCamera(UnityEngine.Camera gameCamera)
        {
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            Canvas.worldCamera = gameCamera;
        }

        public void Show(UnityEngine.Camera gameCamera)
        {
            gameObject.SetActive(true);
            BindCamera(gameCamera);
            SetProgress(0f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetProgress(float ratio)
        {
            var clamped = Mathf.Clamp01(ratio);
            var percent = Mathf.RoundToInt(clamped * 100f);
            _progressSlider.SetValueWithoutNotify(clamped);
            _progressText.text = string.Format(_progressTextFormat, percent);
        }
    }
}
