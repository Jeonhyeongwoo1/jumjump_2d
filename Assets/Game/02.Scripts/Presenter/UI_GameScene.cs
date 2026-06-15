using System.Collections;
using TMPro;
using UnityEngine;
using JumJump.Util;

namespace JumJump.Presenter
{
    public sealed class UI_GameScene : BaseSceneUI
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private GameObject _startCountdownPanel;
        [SerializeField] private TMP_Text _startCountdownText;
        [SerializeField] private float _scorePopDuration = 0.18f;
        [SerializeField] private float _scoreSettleDuration = 0.12f;
        [SerializeField] private float _scorePopScale = 1.18f;
        [SerializeField] private Color _scoreFlashColor = new Color(1f, 0.82f, 0.25f, 1f);

        private Coroutine _scoreAnimationRoutine;
        private Vector3 _scoreTextDefaultScale;
        private Color _scoreTextDefaultColor;

        protected override void Awake()
        {
            base.Awake();
            _scoreTextDefaultScale = _scoreText.rectTransform.localScale;
            _scoreTextDefaultColor = _scoreText.color;
            HideStartCountdown();
        }

        public void SetScore(int score) => _scoreText.text = score.ToString();
        public void SetScoreAnimated(int score)
        {
            SetScore(score);
            RestartScoreAnimation();
        }

        public void SetGold(int gold) => _goldText.text = gold.ToString();

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

        private void RestartScoreAnimation()
        {
            if (_scoreAnimationRoutine != null)
            {
                StopCoroutine(_scoreAnimationRoutine);
            }

            _scoreAnimationRoutine = StartCoroutine(ScoreAnimationRoutine());
        }

        private IEnumerator ScoreAnimationRoutine()
        {
            var popDuration = Mathf.Max(0.01f, _scorePopDuration);
            var settleDuration = Mathf.Max(0.01f, _scoreSettleDuration);
            var elapsed = 0f;
            var popScale = _scoreTextDefaultScale * Mathf.Max(1f, _scorePopScale);

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / popDuration);
                var eased = Mathf.SmoothStep(0f, 1f, normalized);
                _scoreText.rectTransform.localScale = Vector3.Lerp(_scoreTextDefaultScale, popScale, eased);
                _scoreText.color = Color.Lerp(_scoreTextDefaultColor, _scoreFlashColor, eased);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / settleDuration);
                var eased = Mathf.SmoothStep(0f, 1f, normalized);
                _scoreText.rectTransform.localScale = Vector3.Lerp(popScale, _scoreTextDefaultScale, eased);
                _scoreText.color = Color.Lerp(_scoreFlashColor, _scoreTextDefaultColor, eased);
                yield return null;
            }

            ResetScoreTextAnimation();
            _scoreAnimationRoutine = null;
        }

        private void ResetScoreTextAnimation()
        {
            _scoreText.rectTransform.localScale = _scoreTextDefaultScale;
            _scoreText.color = _scoreTextDefaultColor;
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

        private void OnDisable()
        {
            if (_scoreAnimationRoutine != null)
            {
                StopCoroutine(_scoreAnimationRoutine);
                _scoreAnimationRoutine = null;
            }

            ResetScoreTextAnimation();
        }
    }
}
