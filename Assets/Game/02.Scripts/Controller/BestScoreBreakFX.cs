using System;
using System.Collections;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class BestScoreBreakFX : MonoBehaviour
    {
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer _markerFlashRenderer;
        [SerializeField] private SpriteRenderer _softGlowRenderer;
        [SerializeField] private SpriteRenderer _goldRingRenderer;
        [SerializeField] private SpriteRenderer _celebrationBurstRenderer;
        [SerializeField] private SpriteRenderer _newBestTextRenderer;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _starSparkles;
        [SerializeField] private ParticleSystem _seedConfetti;

        [Header("Text Sprites")]
        [SerializeField] private Sprite _newBestTextEnglishSprite;
        [SerializeField] private Sprite _newBestTextKoreanSprite;

        [Header("Options")]
        [SerializeField] private bool _useKoreanText;
        [SerializeField] private bool _autoDestroy = true;
        [SerializeField] private float _destroyDelay = 1.3f;
        [SerializeField] private float _effectScale = 1f;
        [SerializeField] private float _textPopupYOffset = 0.8f;
        [SerializeField] private float _textMoveUpDistance = 0.35f;
        [SerializeField] private float _ringStartScale = 0.4f;
        [SerializeField] private float _ringEndScale = 1.35f;
        [SerializeField] private float _glowMaxAlpha = 0.75f;
        [SerializeField] private float _ringMaxAlpha = 0.9f;
        [SerializeField] private float _markerFlashMaxAlpha = 0.8f;

        private Coroutine _playRoutine;
        private Action<BestScoreBreakFX> _onReturnedToPool = _ => { };
        private bool _hasPoolBinding;
        private bool _isPlaying;

        public void Bind(Action<BestScoreBreakFX> onReturnedToPool)
        {
            _onReturnedToPool = onReturnedToPool;
            _hasPoolBinding = true;
        }

        public void Play(Vector3 worldPosition)
        {
            StopActiveRoutine();
            transform.position = worldPosition;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, _effectScale);
            gameObject.SetActive(true);
            StopParticles();
            ResetVisuals();
            ApplyTextSprite();
            _isPlaying = true;
            _playRoutine = StartCoroutine(PlayRoutine());
        }

        public void PlayAt(Transform target)
        {
            if (target == null)
            {
                return;
            }

            Play(target.position);
        }

        public void StopAndClear()
        {
            StopActiveRoutine();
            StopParticles();
            ResetVisuals();
            _isPlaying = false;
        }

        public void SetPooled()
        {
            _isPlaying = false;
            StopActiveRoutine();
            StopParticles();
            ResetVisuals();
            gameObject.SetActive(false);
        }

        public void SetTextLanguage(bool korean)
        {
            _useKoreanText = korean;
            ApplyTextSprite();
        }

        private IEnumerator PlayRoutine()
        {
            var elapsed = 0f;
            var totalDuration = Mathf.Max(1.05f, _destroyDelay);
            var starPlayed = false;
            var seedPlayed = false;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                UpdateMarkerFlash(elapsed);
                UpdateSoftGlow(elapsed);
                UpdateCelebrationBurst(elapsed);
                UpdateGoldRing(elapsed);
                UpdateNewBestText(elapsed);

                if (!starPlayed && elapsed >= 0.12f)
                {
                    starPlayed = true;
                    RestartParticle(_starSparkles);
                }

                if (!seedPlayed && elapsed >= 0.16f)
                {
                    seedPlayed = true;
                    RestartParticle(_seedConfetti);
                }

                yield return null;
            }

            _playRoutine = null;
            ResetVisuals();
            CompletePlayback();
        }

        private void UpdateMarkerFlash(float elapsed)
        {
            UpdatePulseRenderer(
                _markerFlashRenderer,
                elapsed,
                0f,
                0.45f,
                Vector3.one * 0.7f,
                Vector3.one * 1.05f,
                _markerFlashMaxAlpha,
                0.35f);
        }

        private void UpdateSoftGlow(float elapsed)
        {
            UpdatePulseRenderer(
                _softGlowRenderer,
                elapsed,
                0f,
                0.8f,
                Vector3.one * 0.6f,
                Vector3.one * 1.15f,
                _glowMaxAlpha,
                0.35f);
        }

        private void UpdateCelebrationBurst(float elapsed)
        {
            UpdateFadeRenderer(
                _celebrationBurstRenderer,
                elapsed,
                0f,
                0.35f,
                Vector3.one * 0.7f,
                Vector3.one * 1.15f,
                1f);
        }

        private void UpdateGoldRing(float elapsed)
        {
            UpdateFadeRenderer(
                _goldRingRenderer,
                elapsed,
                0.08f,
                0.55f,
                Vector3.one * Mathf.Max(0.01f, _ringStartScale),
                Vector3.one * Mathf.Max(_ringStartScale, _ringEndScale),
                _ringMaxAlpha);
        }

        private void UpdateNewBestText(float elapsed)
        {
            var renderer = _newBestTextRenderer;
            var localElapsed = elapsed - 0.22f;
            if (localElapsed < 0f)
            {
                return;
            }

            var duration = 0.8f;
            if (localElapsed >= duration)
            {
                SetRendererVisible(renderer, false);
                return;
            }

            SetRendererVisible(renderer, true);
            var normalized = Mathf.Clamp01(localElapsed / duration);
            var textTransform = renderer.transform;
            var basePosition = Vector3.up * _textPopupYOffset;
            textTransform.localPosition = Vector3.Lerp(
                basePosition,
                basePosition + Vector3.up * _textMoveUpDistance,
                Mathf.SmoothStep(0f, 1f, normalized));

            if (normalized < 0.24f)
            {
                var pop = Mathf.Clamp01(normalized / 0.24f);
                SetRendererAlpha(renderer, Mathf.SmoothStep(0f, 1f, pop));
                textTransform.localScale = Vector3.Lerp(Vector3.one * 0.7f, Vector3.one * 1.15f, EaseOut(pop));
                return;
            }

            if (normalized < 0.6f)
            {
                var settle = Mathf.Clamp01((normalized - 0.24f) / 0.36f);
                SetRendererAlpha(renderer, 1f);
                textTransform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, Mathf.SmoothStep(0f, 1f, settle));
                return;
            }

            var fade = Mathf.Clamp01((normalized - 0.6f) / 0.4f);
            SetRendererAlpha(renderer, Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, fade)));
            textTransform.localScale = Vector3.one;
        }

        private void UpdatePulseRenderer(
            SpriteRenderer renderer,
            float elapsed,
            float startTime,
            float duration,
            Vector3 startScale,
            Vector3 endScale,
            float maxAlpha,
            float fadeInPortion)
        {
            var localElapsed = elapsed - startTime;
            if (localElapsed < 0f)
            {
                return;
            }

            if (localElapsed >= duration)
            {
                SetRendererVisible(renderer, false);
                return;
            }

            SetRendererVisible(renderer, true);
            var normalized = Mathf.Clamp01(localElapsed / duration);
            renderer.transform.localScale = Vector3.Lerp(startScale, endScale, EaseOut(normalized));

            var alpha = normalized <= fadeInPortion
                ? Mathf.Lerp(0f, maxAlpha, Mathf.Clamp01(normalized / fadeInPortion))
                : Mathf.Lerp(maxAlpha, 0f, Mathf.Clamp01((normalized - fadeInPortion) / (1f - fadeInPortion)));
            SetRendererAlpha(renderer, alpha);
        }

        private void UpdateFadeRenderer(
            SpriteRenderer renderer,
            float elapsed,
            float startTime,
            float duration,
            Vector3 startScale,
            Vector3 endScale,
            float startAlpha)
        {
            var localElapsed = elapsed - startTime;
            if (localElapsed < 0f)
            {
                return;
            }

            if (localElapsed >= duration)
            {
                SetRendererVisible(renderer, false);
                return;
            }

            SetRendererVisible(renderer, true);
            var normalized = Mathf.Clamp01(localElapsed / duration);
            var eased = EaseOut(normalized);
            renderer.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            SetRendererAlpha(renderer, Mathf.Lerp(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, normalized)));
        }

        private void ApplyTextSprite()
        {
            var renderer = _newBestTextRenderer;
            var selectedSprite = _useKoreanText ? _newBestTextKoreanSprite : _newBestTextEnglishSprite;
            renderer.sprite = selectedSprite != null ? selectedSprite : _newBestTextEnglishSprite;
        }

        private void ResetVisuals()
        {
            ResetRenderer(_markerFlashRenderer, Vector3.zero, Vector3.one * 0.7f);
            ResetRenderer(_softGlowRenderer, Vector3.zero, Vector3.one * 0.6f);
            ResetRenderer(_goldRingRenderer, Vector3.zero, Vector3.one * Mathf.Max(0.01f, _ringStartScale));
            ResetRenderer(_celebrationBurstRenderer, Vector3.zero, Vector3.one * 0.7f);
            ResetRenderer(_newBestTextRenderer, Vector3.up * _textPopupYOffset, Vector3.one * 0.7f);
        }

        private void ResetRenderer(SpriteRenderer renderer, Vector3 localPosition, Vector3 localScale)
        {
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = localScale;
            SetRendererAlpha(renderer, 0f);
            SetRendererVisible(renderer, false);
        }

        private void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            var color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private void SetRendererVisible(SpriteRenderer renderer, bool visible)
        {
            renderer.enabled = visible;
        }

        private void StopParticles()
        {
            StopParticle(_starSparkles);
            StopParticle(_seedConfetti);
        }

        private void RestartParticle(ParticleSystem particleSystem)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }

        private void StopParticle(ParticleSystem particleSystem)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void StopActiveRoutine()
        {
            var routine = _playRoutine;
            if (routine == null)
            {
                return;
            }

            StopCoroutine(routine);
            _playRoutine = null;
        }

        private void CompletePlayback()
        {
            if (!_isPlaying)
            {
                return;
            }

            _isPlaying = false;
            if (_hasPoolBinding)
            {
                _onReturnedToPool.Invoke(this);
                return;
            }

            if (_autoDestroy)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        private float EaseOut(float value)
        {
            var clamped = Mathf.Clamp01(value);
            return 1f - (1f - clamped) * (1f - clamped);
        }

        private void OnDisable()
        {
            StopActiveRoutine();
            StopParticles();
        }
    }
}
