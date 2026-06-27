using System;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class ComboPlatformBurstFX : MonoBehaviour
    {
        private const float Duration = 0.48f;
        private const float RingMaxAlpha = 0.85f;
        private const float SparkleMaxAlpha = 0.95f;

        private SpriteRenderer _ringRenderer;
        private SpriteRenderer[] _sparkleRenderers;
        private Vector3[] _sparkleDirections;
        private Action<ComboPlatformBurstFX> _onReturnedToPool = _ => { };
        private bool _isPlaying;
        private float _elapsed;
        private float _comboScale;

        public void Bind(
            SpriteRenderer ringRenderer,
            SpriteRenderer[] sparkleRenderers,
            Action<ComboPlatformBurstFX> onReturnedToPool)
        {
            _ringRenderer = ringRenderer;
            _sparkleRenderers = sparkleRenderers;
            _sparkleDirections = new Vector3[_sparkleRenderers.Length];
            _onReturnedToPool = onReturnedToPool;
            CacheSparkleDirections();
        }

        public void Play(Vector3 worldPosition, int comboCount)
        {
            _elapsed = 0f;
            _comboScale = 1f + Mathf.Min(Mathf.Max(0, comboCount), 8) * 0.045f;
            _isPlaying = true;
            transform.position = worldPosition;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
            SetRendererVisible(_ringRenderer, true);

            for (var i = 0; i < _sparkleRenderers.Length; i++)
            {
                SetRendererVisible(_sparkleRenderers[i], true);
            }

            UpdateVisuals(0f);
        }

        public void SetPooled()
        {
            _isPlaying = false;
            _elapsed = 0f;
            SetRendererVisible(_ringRenderer, false);
            SetRendererAlpha(_ringRenderer, 0f);

            for (var i = 0; i < _sparkleRenderers.Length; i++)
            {
                SetRendererVisible(_sparkleRenderers[i], false);
                SetRendererAlpha(_sparkleRenderers[i], 0f);
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            var normalized = Mathf.Clamp01(_elapsed / Duration);
            UpdateVisuals(normalized);

            if (normalized >= 1f)
            {
                CompletePlayback();
            }
        }

        private void UpdateVisuals(float normalized)
        {
            var eased = EaseOut(normalized);
            _ringRenderer.transform.localScale = Vector3.Lerp(
                Vector3.one * 0.45f * _comboScale,
                Vector3.one * 1.42f * _comboScale,
                eased);
            SetRendererAlpha(_ringRenderer, Mathf.Lerp(RingMaxAlpha, 0f, Mathf.SmoothStep(0f, 1f, normalized)));

            for (var i = 0; i < _sparkleRenderers.Length; i++)
            {
                UpdateSparkle(_sparkleRenderers[i], _sparkleDirections[i], normalized);
            }
        }

        private void UpdateSparkle(SpriteRenderer sparkleRenderer, Vector3 direction, float normalized)
        {
            var delay = 0.08f;
            var sparkleT = Mathf.Clamp01((normalized - delay) / (1f - delay));
            sparkleRenderer.transform.localPosition = direction * Mathf.Lerp(0.08f, 0.52f * _comboScale, EaseOut(sparkleT));
            sparkleRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.14f, 0.28f * _comboScale, EaseOut(sparkleT));
            SetRendererAlpha(sparkleRenderer, Mathf.Lerp(SparkleMaxAlpha, 0f, Mathf.SmoothStep(0f, 1f, sparkleT)));
        }

        private void CacheSparkleDirections()
        {
            for (var i = 0; i < _sparkleDirections.Length; i++)
            {
                var angle = (Mathf.PI * 2f / Mathf.Max(1, _sparkleDirections.Length)) * i + Mathf.PI * 0.25f;
                _sparkleDirections[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.65f, 0f);
            }
        }

        private void CompletePlayback()
        {
            if (!_isPlaying)
            {
                return;
            }

            _isPlaying = false;
            _onReturnedToPool.Invoke(this);
        }

        private void SetRendererVisible(SpriteRenderer renderer, bool isVisible)
        {
            renderer.enabled = isVisible;
        }

        private void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            var color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private float EaseOut(float value)
        {
            var clamped = Mathf.Clamp01(value);
            return 1f - (1f - clamped) * (1f - clamped);
        }
    }
}
