using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer;

namespace JumJump.Controller
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Player : MonoBehaviour
    {
        public bool IsJumping => _isJumping;
        public bool CanLand => _isJumping &&
                               NormalizedJumpTime >= _configData.PlayerLandingEnabledNormalizedTime;
        public bool IsDescending => _isJumping && _rigidbody != null && _rigidbody.linearVelocity.y <= 0f;
        public Vector3 Position => transform.position;
        public Vector3 PreviousPosition => _previousPosition;
        public float BottomY => ResolveBottomY(Position);
        public float PreviousBottomY => ResolveBottomY(_previousPosition);
        public float GroundContactOffset => ResolveGroundContactOffset();
        public PlayerStateType State => _state;

        [SerializeField] private Collider2D _bodyCollider;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Animator _animator;

        private bool _isJumping;
        private float _jumpElapsed;
        private int _isJumpingAnimatorParameterHash;
        private PlayerStateType _state;
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
            PlaceRigidbody(_groundPosition);
            ChangeState(PlayerStateType.Idle);
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

        public void LandOnStackedPlatform(PlatformController platform, Vector3 landingPosition)
        {
            if (platform == null)
            {
                return;
            }

            PlaceAtGroundPosition(landingPosition);
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
            ApplyJumpVelocity();
            ChangeState(PlayerStateType.Jump);
            _eventBus.Publish(new PlayerJumpStartedEvent());
        }

        private void PlaceAtGroundPosition(Vector3 groundPosition)
        {
            _isJumping = false;
            _jumpElapsed = 0f;
            _groundPosition = groundPosition;
            _previousPosition = _groundPosition;
            PlaceRigidbody(_groundPosition);
            ChangeState(PlayerStateType.Idle);
        }

        private void ChangeState(PlayerStateType state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            ApplyAnimatorState();
        }

        private void ApplyAnimatorState()
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetBool(_isJumpingAnimatorParameterHash, _state == PlayerStateType.Jump);
        }

        private void PlaceRigidbody(Vector3 position)
        {
            if (_rigidbody == null)
            {
                transform.position = position;
                return;
            }

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.gravityScale = 0f;
            _rigidbody.position = position;
            transform.position = position;
        }

        private void ApplyJumpVelocity()
        {
            if (_rigidbody == null)
            {
                return;
            }

            var halfDuration = Mathf.Max(0.01f, _configData.PlayerJumpDuration * 0.5f);
            var jumpHeight = Mathf.Max(0.01f, _configData.PlayerJumpHeight);
            var gravityMagnitude = Mathf.Max(0.01f, -Physics2D.gravity.y);
            var jumpVelocity = 2f * jumpHeight / halfDuration;
            var gravityScale = jumpVelocity / (gravityMagnitude * halfDuration);

            _rigidbody.gravityScale = gravityScale;
            _rigidbody.linearVelocity = new Vector2(0f, jumpVelocity);
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

        private void FixedUpdate()
        {
            if (!_isJumping)
            {
                return;
            }

            _previousPosition = transform.position;
            _jumpElapsed += Time.fixedDeltaTime;
        }

        private void Awake()
        {
            if (_bodyCollider == null)
            {
                _bodyCollider = GetComponentInChildren<Collider2D>();
            }

            if (_rigidbody == null && !TryGetComponent(out _rigidbody))
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            if (_rigidbody != null)
            {
                _rigidbody.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody.gravityScale = 0f;
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            _isJumpingAnimatorParameterHash = Animator.StringToHash("IsJumping");
            _spawnPosition = transform.position;
            _groundPosition = _spawnPosition;
            _previousPosition = _spawnPosition;
            _state = PlayerStateType.Idle;
            ApplyAnimatorState();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryResolvePlatformTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryResolvePlatformTrigger(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryResolvePlatformCollision(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryResolvePlatformCollision(collision.collider);
        }

        private void TryResolvePlatformTrigger(Collider2D other)
        {
            if (!_isJumping || !IsDescending)
            {
                return;
            }

            if (!other.TryGetComponent(out PlatformController platform))
            {
                return;
            }

            platform.TryResolveLanding(this);
        }

        private void TryResolvePlatformCollision(Collider2D other)
        {
            if (!_isJumping || !IsDescending || other == null)
            {
                return;
            }

            var platform = other.GetComponent<PlatformController>();
            if (platform == null)
            {
                return;
            }

            platform.TryResolveStackedLanding(this);
        }
    }
}
