using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer;

namespace JumJump.Controller
{
    public sealed class Player : MonoBehaviour
    {
        public bool IsJumping => _isJumping;
        public bool CanLand => _isJumping &&
                               NormalizedJumpTime >= _configData.PlayerLandingEnabledNormalizedTime;
        public Vector3 Position => transform.position;

        private bool _isJumping;
        private float _jumpElapsed;
        private Vector3 _spawnPosition;
        private Vector3 _groundPosition;
        private IEventBus _eventBus;
        private GameConfigData _configData;

        private float NormalizedJumpTime => Mathf.Clamp01(_jumpElapsed / Mathf.Max(0.01f, _configData.PlayerJumpDuration));

        [Inject]
        public void Construct(IEventBus eventBus, GameConfigData configData)
        {
            _eventBus = eventBus;
            _configData = configData;
        }

        public void ResetForRound()
        {
            _isJumping = false;
            _jumpElapsed = 0f;
            _groundPosition = _spawnPosition;
            transform.position = _groundPosition;
        }

        public void PlaceOnPlatform(PlatformController platform)
        {
            _isJumping = false;

            if (platform == null)
            {
                return;
            }

            _groundPosition = platform.GetLandingPosition(_configData.PlayerVerticalOffset);
            transform.position = _groundPosition;
        }

        public void LandOnPlatform(PlatformController platform)
        {
            PlaceOnPlatform(platform);
            _eventBus.Publish(new PlayerLandedEvent(platform));
        }

        private void OnPlayerJumpRequested(in PlayerJumpRequestedEvent ev)
        {
            if (_isJumping)
            {
                return;
            }

            _jumpElapsed = 0f;
            _groundPosition = transform.position;
            _isJumping = true;
            _eventBus.Publish(new PlayerJumpStartedEvent());
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
            var normalizedTime = NormalizedJumpTime;
            var arcHeight = 4f * Mathf.Max(0f, _configData.PlayerJumpHeight) * normalizedTime * (1f - normalizedTime);
            var nextPosition = _groundPosition;
            nextPosition.y += arcHeight;
            transform.position = nextPosition;

            if (normalizedTime >= 1f)
            {
                _isJumping = false;
                transform.position = _groundPosition;
            }
        }

        private void Awake()
        {
            _spawnPosition = transform.position;
            _groundPosition = _spawnPosition;
        }
    }
}
