using UnityEngine;

namespace JumJump.Controller
{
    internal struct PlayerRocketBoostRuntime
    {
        private float _elapsed;
        private PlatformController _targetPlatform;
        private Vector3 _startGroundPosition;
        private Vector3 _boostEndGroundPosition;
        private Vector3 _landingGroundPosition;

        public PlatformController TargetPlatform => _targetPlatform;
        public Vector3 LandingGroundPosition => _landingGroundPosition;

        public void Reset()
        {
            _elapsed = 0f;
            _targetPlatform = null;
            _startGroundPosition = Vector3.zero;
            _boostEndGroundPosition = Vector3.zero;
            _landingGroundPosition = Vector3.zero;
        }

        public void Start(
            PlatformController targetPlatform,
            Vector3 startGroundPosition,
            Vector3 landingGroundPosition,
            float dropHeight)
        {
            _elapsed = 0f;
            _targetPlatform = targetPlatform;
            _startGroundPosition = startGroundPosition;
            _landingGroundPosition = landingGroundPosition;
            _boostEndGroundPosition = landingGroundPosition + Vector3.up * Mathf.Max(0f, dropHeight);
        }

        public bool Tick(float deltaTime, float duration, out Vector3 nextGroundPosition)
        {
            _elapsed += deltaTime;
            var normalizedTime = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, duration));
            var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            nextGroundPosition = Vector3.Lerp(_startGroundPosition, _boostEndGroundPosition, easedTime);
            return normalizedTime >= 1f;
        }
    }
}
