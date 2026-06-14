using TMPro;
using UnityEngine;
using JumJump.Util;

namespace JumJump.Presenter
{
    public sealed class UI_GameScene : BaseSceneUI
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _comboScoreText;
        [SerializeField] private GameObject _startCountdownPanel;
        [SerializeField] private TMP_Text _startCountdownText;

        protected override void Awake()
        {
            base.Awake();
            HideStartCountdown();
        }

        public void SetScore(int score) => _scoreText.text = score.ToString();
        public void SetComboScore(int comboScore) => _comboScoreText.text = comboScore.ToString();

        public void ShowStartCountdown(int seconds)
        {
            _startCountdownPanel.SetActive(true);
            SetStartCountdown(seconds);
            SetStartCountdownProgress(0f);
        }

        public void SetStartCountdown(int seconds)
        {
            _startCountdownText.text = seconds.ToString();
        }

        public void SetStartCountdownProgress(float normalized)
        {
            var progress = Mathf.Clamp01(normalized);
            var scale = ResolveStartCountdownScale(progress);
            _startCountdownText.rectTransform.localScale = Vector3.one * scale;
            _startCountdownText.alpha = ResolveStartCountdownAlpha(progress);
        }

        public void HideStartCountdown()
        {
            _startCountdownText.rectTransform.localScale = Vector3.one;
            _startCountdownText.alpha = 1f;
            _startCountdownPanel.SetActive(false);
        }

        private float ResolveStartCountdownScale(float progress)
        {
            if (progress < GameConst.UI.StartCountdownPopInDuration)
            {
                var popT = progress / GameConst.UI.StartCountdownPopInDuration;
                return Mathf.Lerp(
                    GameConst.UI.StartCountdownScaleFrom,
                    GameConst.UI.StartCountdownScalePop,
                    Mathf.SmoothStep(0f, 1f, popT));
            }

            if (progress < GameConst.UI.StartCountdownSettleDuration)
            {
                var settleT = (progress - GameConst.UI.StartCountdownPopInDuration) /
                        (GameConst.UI.StartCountdownSettleDuration - GameConst.UI.StartCountdownPopInDuration);
                return Mathf.Lerp(
                    GameConst.UI.StartCountdownScalePop,
                    GameConst.UI.StartCountdownScaleSettle,
                    Mathf.SmoothStep(0f, 1f, settleT));
            }

            if (progress < GameConst.UI.StartCountdownFadeOutStart)
            {
                return GameConst.UI.StartCountdownScaleSettle;
            }

            var fadeT = (progress - GameConst.UI.StartCountdownFadeOutStart) /
                        (1f - GameConst.UI.StartCountdownFadeOutStart);
            return Mathf.Lerp(
                GameConst.UI.StartCountdownScaleSettle,
                GameConst.UI.StartCountdownScaleOut,
                Mathf.SmoothStep(0f, 1f, fadeT));
        }

        private float ResolveStartCountdownAlpha(float progress)
        {
            if (progress < GameConst.UI.StartCountdownPopInDuration)
            {
                var popT = progress / GameConst.UI.StartCountdownPopInDuration;
                return Mathf.SmoothStep(0f, 1f, popT);
            }

            if (progress < GameConst.UI.StartCountdownFadeOutStart)
            {
                return 1f;
            }

            var fadeT = (progress - GameConst.UI.StartCountdownFadeOutStart) /
                    (1f - GameConst.UI.StartCountdownFadeOutStart);
            return Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, fadeT));
        }
    }
}
