using System;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class ComboPlatformAuraFX : MonoBehaviour
    {
        private const float FadeInDuration = 0.18f;
        private const float ActiveDuration = 0.55f;
        private const float FadeOutDuration = 0.85f;
        private const float MaxAlpha = 0.42f;
        private const float PulseScale = 0.08f;
        private const float PulseSpeed = 5.2f;

        private SpriteRenderer _auraRenderer;
        private Action<ComboPlatformAuraFX> _onReturnedToPool = _ => { };
        private bool _isActive;
        private bool _isFadingOut;
        private float _elapsed;
        private float _fadeStartAlpha;
        private Vector3 _baseScale;

        public void Bind(SpriteRenderer auraRenderer, Action<ComboPlatformAuraFX> onReturnedToPool)
        {
            _auraRenderer = auraRenderer;
            _onReturnedToPool = onReturnedToPool;
        }

        public void Play(Vector3 worldPosition, int comboCount)
        {
            _elapsed = 0f;
            _fadeStartAlpha = MaxAlpha;
            _isActive = true;
            _isFadingOut = false;
            transform.position = worldPosition;
            _baseScale = ResolveBaseScale(comboCount);
            transform.localScale = _baseScale;
            gameObject.SetActive(true);
            _auraRenderer.enabled = true;
            SetAuraAlpha(0f);
        }

        public void FadeOut()
        {
            if (!_isActive)
            {
                return;
            }

            _elapsed = 0f;
            _isFadingOut = true;
            _fadeStartAlpha = _auraRenderer.color.a;
        }

        public void SetPooled()
        {
            _isActive = false;
            _isFadingOut = false;
            _elapsed = 0f;
            _auraRenderer.enabled = false;
            SetAuraAlpha(0f);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_isFadingOut)
            {
                UpdateFadeOut();
                return;
            }

            UpdateActiveAura();
        }

        private void UpdateActiveAura()
        {
            var fadeIn = Mathf.Clamp01(_elapsed / FadeInDuration);
            var pulse = 1f + Mathf.Sin(_elapsed * PulseSpeed) * PulseScale;
            transform.localScale = _baseScale * pulse;
            SetAuraAlpha(Mathf.SmoothStep(0f, MaxAlpha, fadeIn));

            if (_elapsed >= ActiveDuration)
            {
                FadeOut();
            }
        }

        private void UpdateFadeOut()
        {
            var normalized = Mathf.Clamp01(_elapsed / FadeOutDuration);
            transform.localScale = Vector3.Lerp(_baseScale, _baseScale * 1.18f, normalized);
            SetAuraAlpha(Mathf.Lerp(_fadeStartAlpha, 0f, Mathf.SmoothStep(0f, 1f, normalized)));

            if (normalized >= 1f)
            {
                CompletePlayback();
            }
        }

        private Vector3 ResolveBaseScale(int comboCount)
        {
            var comboScale = 1f + Mathf.Min(Mathf.Max(0, comboCount), 8) * 0.035f;
            return new Vector3(1.35f * comboScale, 0.34f * comboScale, 1f);
        }

        private void SetAuraAlpha(float alpha)
        {
            _auraRenderer.color = new Color(1f, 0.82f, 0.24f, Mathf.Clamp01(alpha));
        }

        private void CompletePlayback()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _onReturnedToPool.Invoke(this);
        }
    }
}
