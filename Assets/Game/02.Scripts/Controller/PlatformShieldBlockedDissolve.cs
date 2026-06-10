using UnityEngine;

namespace JumJump.Controller
{
    internal struct PlatformShieldBlockedDissolve
    {
        private float _elapsed;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;

        public void Reset()
        {
            _elapsed = 0f;
            _startPosition = Vector3.zero;
            _targetPosition = Vector3.zero;
        }

        public void Start(Vector3 startPosition, float retreatDirectionX, float retreatDistance)
        {
            _elapsed = 0f;
            _startPosition = startPosition;
            _targetPosition = startPosition + new Vector3(retreatDirectionX * retreatDistance, 0f, 0f);
        }

        public bool Tick(float deltaTime, float duration, out Vector3 nextPosition, out float alpha)
        {
            _elapsed += deltaTime;
            var normalizedTime = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, duration));
            var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            nextPosition = Vector3.Lerp(_startPosition, _targetPosition, easedTime);
            alpha = 1f - easedTime;
            return normalizedTime >= 1f;
        }
    }
}
