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
        public bool IsDescending => _isJumping && _rigidbody.linearVelocity.y <= 0f;
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
        private bool _isGameOverKnockback;
        private float _jumpElapsed;
        private int _isJumpingAnimatorParameterHash;
        private int _isDeadAnimatorParameterHash;
        private int _idleAnimatorStateHash;
        private PlayerStateType _state;
        private Vector3 _spawnPosition;
        private Vector3 _groundPosition;
        private Vector3 _previousPosition;
        private Vector3 _defaultLocalScale;
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
            _isGameOverKnockback = false;
            _jumpElapsed = 0f;
            _groundPosition = _spawnPosition;
            _previousPosition = _groundPosition;
            ApplyFacingScale(1f);
            PlaceRigidbody(_groundPosition);
            ChangeState(PlayerStateType.Idle);
        }

        public void LandOnPlatform(PlatformController platform, Vector3 landingPosition)
        {
            PlaceAtGroundPosition(landingPosition);
            _eventBus.Publish(new PlayerLandedEvent(platform));
        }

        public void LandOnStackedPlatform(PlatformController platform, Vector3 landingPosition)
        {
            PlaceAtGroundPosition(landingPosition);
        }

        private void OnPlayerJumpRequested(in PlayerJumpRequestedEvent ev)
        {
            if (_isJumping || _isGameOverKnockback)
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

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            PlayGameOverKnockback(ev.KnockbackDirection);
        }

        private void PlaceAtGroundPosition(Vector3 groundPosition)
        {
            _isJumping = false;
            _isGameOverKnockback = false;
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

            var previousState = _state;
            _state = state;
            ApplyAnimatorState();

            if (previousState == PlayerStateType.Knockback && state == PlayerStateType.Idle)
            {
                PlayIdleAnimatorState();
            }
        }

        private void ApplyAnimatorState()
        {
            _animator.SetBool(
                _isJumpingAnimatorParameterHash,
                _state == PlayerStateType.Jump);
            _animator.SetBool(
                _isDeadAnimatorParameterHash,
                _state == PlayerStateType.Knockback);
        }

        private void PlayIdleAnimatorState()
        {
            _animator.Play(_idleAnimatorStateHash, 0, 0f);
        }

        private void PlaceRigidbody(Vector3 position)
        {
            _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.gravityScale = 0f;
            _rigidbody.position = position;
            transform.position = position;
        }

        private void ApplyJumpVelocity()
        {
            var halfDuration = Mathf.Max(0.01f, _configData.PlayerJumpDuration * 0.5f);
            var jumpHeight = Mathf.Max(0.01f, _configData.PlayerJumpHeight);
            var gravityMagnitude = Mathf.Max(0.01f, -Physics2D.gravity.y);
            var jumpVelocity = 2f * jumpHeight / halfDuration;
            var gravityScale = jumpVelocity / (gravityMagnitude * halfDuration);

            _rigidbody.gravityScale = gravityScale;
            _rigidbody.linearVelocity = new Vector2(0f, jumpVelocity);
        }

        private void PlayGameOverKnockback(Vector2 knockbackDirection)
        {
            if (_isGameOverKnockback)
            {
                return;
            }

            _isJumping = false;
            _isGameOverKnockback = true;
            _jumpElapsed = 0f;
            _previousPosition = transform.position;
            ChangeState(PlayerStateType.Knockback);

            var directionX = ResolveKnockbackDirectionX(knockbackDirection);
            ApplyFacingScale(directionX);
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.gravityScale = Mathf.Max(0f, _configData.PlayerGameOverKnockbackGravityScale);
            _rigidbody.linearVelocity = new Vector2(
                directionX * Mathf.Max(0f, _configData.PlayerGameOverKnockbackHorizontalSpeed),
                Mathf.Max(0f, _configData.PlayerGameOverKnockbackUpwardSpeed));
        }

        private float ResolveKnockbackDirectionX(Vector2 knockbackDirection)
        {
            if (Mathf.Approximately(knockbackDirection.x, 0f))
            {
                return 1f;
            }

            return Mathf.Sign(knockbackDirection.x);
        }

        private void ApplyFacingScale(float directionX)
        {
            var nextScale = transform.localScale;
            var scaleX = Mathf.Max(0.0001f, Mathf.Abs(_defaultLocalScale.x));
            nextScale.x = ResolveKnockbackDirectionX(new Vector2(directionX, 0f)) * scaleX;
            transform.localScale = nextScale;
        }

        private float ResolveBottomY(Vector3 position)
        {
            return position.y - GroundContactOffset;
        }

        private float ResolveGroundContactOffset()
        {
            return transform.position.y - _bodyCollider.bounds.min.y;
        }

        private void Start()
        {
            _eventBus.Subscribe<PlayerJumpRequestedEvent>(OnPlayerJumpRequested);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
        }

        private void OnDestroy()
        {
            _eventBus.Unsubscribe<PlayerJumpRequestedEvent>(OnPlayerJumpRequested);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
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
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = 0f;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _isJumpingAnimatorParameterHash = Animator.StringToHash("IsJumping");
            _isDeadAnimatorParameterHash = Animator.StringToHash("IsDead");
            _idleAnimatorStateHash = Animator.StringToHash("Idle");
            _defaultLocalScale = transform.localScale;
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
