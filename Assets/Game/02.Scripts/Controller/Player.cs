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
        public bool IsDescending => _isJumping && BottomY <= PreviousBottomY;
        public Vector3 Position => transform.position;
        public Vector3 PreviousPosition => _previousPosition;
        public float BottomY => ResolveBottomY(Position);
        public float PreviousBottomY => ResolveBottomY(_previousPosition);
        public float GroundContactOffset => ResolveGroundContactOffset();

        [SerializeField] private Collider2D _bodyCollider;

        private bool _isJumping;
        private float _jumpElapsed;
        private Vector3 _spawnPosition;
        private Vector3 _groundPosition;
        private Vector3 _previousPosition;
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
            _previousPosition = _groundPosition;
            transform.position = _groundPosition;
        }

        public void PlaceOnPlatform(PlatformController platform)
        {
            if (platform == null)
            {
                _isJumping = false;
                return;
            }

            PlaceAtGroundPosition(platform.GetLandingPosition(this));
        }

        public void LandOnPlatform(PlatformController platform)
        {
            PlaceOnPlatform(platform);
            _eventBus.Publish(new PlayerLandedEvent(platform));
        }

        public void LandOnPlatform(PlatformController platform, Vector3 landingPosition)
        {
            PlaceAtGroundPosition(landingPosition);
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
            _previousPosition = _groundPosition;
            _isJumping = true;
            _eventBus.Publish(new PlayerJumpStartedEvent());
        }

        private void PlaceAtGroundPosition(Vector3 groundPosition)
        {
            _isJumping = false;
            _jumpElapsed = 0f;
            _groundPosition = groundPosition;
            _previousPosition = _groundPosition;
            transform.position = _groundPosition;
        }

        private float ResolveBottomY(Vector3 position)
        {
            return position.y - GroundContactOffset;
        }

        private float ResolveGroundContactOffset()
        {
            if (_bodyCollider == null)
            {
                return _configData == null ? 0f : Mathf.Max(0f, _configData.PlayerVerticalOffset);
            }

            return transform.position.y - _bodyCollider.bounds.min.y;
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

            _previousPosition = transform.position;
            _jumpElapsed += Time.deltaTime;
            var normalizedTime = NormalizedJumpTime;
            var arcHeight = 4f * Mathf.Max(0f, _configData.PlayerJumpHeight) * normalizedTime * (1f - normalizedTime);
            var nextPosition = _groundPosition;
            nextPosition.y += arcHeight;
            transform.position = nextPosition;

            if (normalizedTime >= 1f)
            {
                transform.position = _groundPosition;
            }
        }

        private void Awake()
        {
            if (_bodyCollider == null)
            {
                _bodyCollider = GetComponent<Collider2D>();
            }

            _spawnPosition = transform.position;
            _groundPosition = _spawnPosition;
            _previousPosition = _spawnPosition;
        }
    }
}
