using UnityEngine;

namespace JumJump.Controller
{
    internal struct PlayerLandingSinkRuntime
    {
        private Vector3 _basePosition;
        private float _elapsed;

        public Vector3 BasePosition => _basePosition;

        public void Reset(Vector3 basePosition)
        {
            _basePosition = basePosition;
            _elapsed = float.PositiveInfinity;
        }

        public void Begin(Vector3 basePosition)
        {
            _basePosition = basePosition;
            _elapsed = 0f;
        }

        public bool Advance(float deltaTime, float duration, out float normalizedTime)
        {
            duration = Mathf.Max(0.01f, duration);
            if (_elapsed >= duration)
            {
                normalizedTime = 1f;
                return false;
            }

            _elapsed += deltaTime;
            normalizedTime = Mathf.Clamp01(_elapsed / duration);
            return true;
        }
    }
}
