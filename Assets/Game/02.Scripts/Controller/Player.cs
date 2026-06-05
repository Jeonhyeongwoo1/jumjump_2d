using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;
using VContainer;

namespace JumJump.Controller
{
    public sealed class Player : MonoBehaviour
    {
        public bool IsJumping => _isJumping;

        [SerializeField] private float _jumpDuration = 0.48f;
        [SerializeField] private float _jumpHeight = 1.35f;
        [SerializeField] private float _characterVerticalOffset = 0.58f;
        [SerializeField] private float _landingVerticalTolerance = 0.35f;

        private bool _isJumping;
        private float _jumpElapsed;
        private Vector3 _jumpStartPosition;
        private Vector3 _jumpEndPosition;
        private PlatformController _currentPlatform;
        private PlatformController _targetPlatform;
        private IEventBus _eventBus;
        private PlatformRegistry _platformRegistry;

        [Inject]
        public void Construct(IEventBus eventBus, PlatformRegistry platformRegistry)
        {
            _eventBus = eventBus;
            _platformRegistry = platformRegistry;
        }

        public void PlaceOnPlatform(PlatformController platform)
        {
            _currentPlatform = platform;
            _targetPlatform = null;
            _isJumping = false;

            if (platform == null)
            {
                return;
            }

            transform.position = platform.GetLandingPosition(_characterVerticalOffset);
        }

        private void OnPlayerJumpRequested(in PlayerJumpRequestedEvent ev)
        {
            if (_isJumping)
            {
                return;
            }

            var baseY = _currentPlatform != null ? _currentPlatform.CenterY : transform.position.y;
            _targetPlatform = _platformRegistry.GetNextPlatformAbove(baseY);

            if (_targetPlatform == null)
            {
                _eventBus.Publish(new PlayerMissedLandingEvent());
                return;
            }

            _jumpElapsed = 0f;
            _jumpStartPosition = transform.position;
            _jumpEndPosition = _targetPlatform.GetLandingPosition(_characterVerticalOffset);
            _isJumping = true;
            _eventBus.Publish(new PlayerJumpStartedEvent());
        }

        private void CompleteJump()
        {
            _isJumping = false;
            transform.position = _jumpEndPosition;

            if (_targetPlatform != null &&
                _targetPlatform.IsLandingPointInside(transform.position, _characterVerticalOffset, _landingVerticalTolerance))
            {
                _currentPlatform = _targetPlatform;
                _eventBus.Publish(new PlayerLandedEvent(_currentPlatform));
                return;
            }

            _eventBus.Publish(new PlayerMissedLandingEvent());
        }

        private void Start()
        {
            if (_eventBus == null)
            {
                Debug.LogError($"[{nameof(Player)}] Missing dependency: {nameof(_eventBus)}.");
                enabled = false;
                return;
            }

            _eventBus.Subscribe<PlayerJumpRequestedEvent>(OnPlayerJumpRequested);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<PlayerJumpRequestedEvent>(OnPlayerJumpRequested);
        }

        private void Update()
        {
            if (!_isJumping)
            {
                return;
            }

            _jumpElapsed += Time.deltaTime;
            var normalizedTime = Mathf.Clamp01(_jumpElapsed / _jumpDuration);
            var arcHeight = 4f * _jumpHeight * normalizedTime * (1f - normalizedTime);
            var nextPosition = Vector3.LerpUnclamped(_jumpStartPosition, _jumpEndPosition, normalizedTime);
            nextPosition.y += arcHeight;
            transform.position = nextPosition;

            if (normalizedTime >= 1f)
            {
                CompleteJump();
            }
        }
    }
}
