using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Util;
using UnityEngine;
using VContainer;

namespace JumJump.Controller
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Player : MonoBehaviour
    {
        public bool IsJumping => _state == PlayerStateType.Jump;
        public bool CanLand => IsJumping &&
                               NormalizedJumpTime >= _configData.PlayerLandingEnabledNormalizedTime;
        public bool IsDescending => IsJumping && _rigidbody.linearVelocity.y <= 0f;
        public Vector3 Position => transform.position;
        public Vector3 PreviousPosition => _previousPosition;
        public float BottomY => ResolveBottomY(Position);
        public float PreviousBottomY => ResolveBottomY(_previousPosition);
        public float GroundContactOffset => ResolveGroundContactOffset();
        public PlayerStateType State => _state;

        [SerializeField] private Collider2D _bodyCollider;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _shieldVisualRoot;
        [SerializeField] private SpriteRenderer _shieldSpriteRenderer;
        [SerializeField] private Animator _shieldAnimator;

        private float _jumpElapsed;
        private int _isJumpingAnimatorParameterHash;
        private int _isDeadAnimatorParameterHash;
        private int _idleAnimatorStateHash;
        private int _shieldIdleAnimatorStateHash;
        private int _shieldBreakAnimatorStateHash;
        private PlayerStateType _state;
        private PlayerShieldRuntime _shield;
        private PlayerRocketBoostRuntime _rocketBoost;
        private PlayerLandingSinkRuntime _landingSink;
        private PlayerKnockbackRuntime _knockback;
        private Vector3 _spawnPosition;
        private Vector3 _groundPosition;
        private Vector3 _previousPosition;
        private Vector3 _defaultLocalScale;
        private IEventBus _eventBus;
        private PlayerConfigData _configData;

        private float NormalizedJumpTime => Mathf.Clamp01(_jumpElapsed / Mathf.Max(0.01f, _configData.PlayerJumpDuration));

        [Inject]
        public void Construct(IEventBus eventBus, PlayerConfigData configData)
        {
            _eventBus = eventBus;
            _configData = configData;
        }

        public void ResetForRound()
        {
            _jumpElapsed = 0f;
            _rocketBoost.Reset();
            _knockback.Reset();
            _groundPosition = _spawnPosition;
            _previousPosition = _groundPosition;
            ApplyFacingScale(1f);
            PlaceRigidbody(_groundPosition);
            ResetLandingSink();
            _shield.Reset(_shieldVisualRoot, _shieldSpriteRenderer);
            ChangeState(PlayerStateType.Idle);
        }

        public void LandOnPlatform(PlatformController platform, Vector3 landingPosition)
        {
            var contactPosition = ResolveGroundContactPosition(landingPosition);
            PlaceAtGroundPosition(landingPosition, true);
            _eventBus.Publish(new PlayerLandedEvent(platform, contactPosition));
        }

        public void LandOnStackedPlatform(PlatformController platform, Vector3 landingPosition)
        {
            PlaceAtGroundPosition(landingPosition, true);
        }

        public void GrantShield()
        {
            _shield.Grant(
                _shieldVisualRoot,
                _shieldSpriteRenderer,
                _shieldAnimator,
                _shieldIdleAnimatorStateHash);
        }

        public bool TryBlockWithShield()
        {
            if (!_shield.TryBlock(_shieldVisualRoot, _shieldAnimator, _shieldBreakAnimatorStateHash))
            {
                return false;
            }

            ReturnToShieldBlockStartPosition();
            return true;
        }

        public void StartRocketBoost(PlatformController targetPlatform, Vector3 targetGroundPosition)
        {
            _jumpElapsed = 0f;
            _knockback.Reset();
            _rocketBoost.Start(
                targetPlatform,
                _groundPosition,
                targetGroundPosition,
                _configData.PlayerRocketDropHeight);
            _previousPosition = _groundPosition;
            ResetLandingSink();
            ChangeState(PlayerStateType.RocketBoost);
        }

        private void OnPlayerJumpRequested(in PlayerJumpRequestedEvent ev)
        {
            if (_state != PlayerStateType.Idle)
            {
                return;
            }

            _jumpElapsed = 0f;
            ResetLandingSink();
            _groundPosition = transform.position;
            _previousPosition = _groundPosition;
            ApplyJumpVelocity();
            ChangeState(PlayerStateType.Jump);
            _eventBus.Publish(new PlayerJumpStartedEvent());
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            PlayGameOverKnockback(ev.KnockbackDirection);
        }

        private void PlaceAtGroundPosition(Vector3 groundPosition, bool playLandingSink)
        {
            _jumpElapsed = 0f;
            _rocketBoost.Reset();
            _knockback.Reset();
            _groundPosition = groundPosition;
            _previousPosition = _groundPosition;
            PlaceRigidbody(_groundPosition);
            ChangeState(PlayerStateType.Idle);

            if (playLandingSink)
            {
                BeginLandingSink(_groundPosition);
            }
            else
            {
                ResetLandingSink();
            }
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
                _state == PlayerStateType.Jump || _state == PlayerStateType.RocketBoost);
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
            _rigidbody.gravityScale = ResolveJumpGravityScale();
            _rigidbody.linearVelocity = new Vector2(0f, ResolveJumpVelocity());
        }

        private void ApplyRocketDropVelocity()
        {
            _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.gravityScale = ResolveJumpGravityScale();
            _rigidbody.linearVelocity = new Vector2(0f, -Mathf.Max(0f, _configData.PlayerRocketDropDownwardSpeed));
        }

        private float ResolveJumpVelocity()
        {
            var halfDuration = Mathf.Max(0.01f, _configData.PlayerJumpDuration * 0.5f);
            var jumpHeight = Mathf.Max(0.01f, _configData.PlayerJumpHeight);
            return 2f * jumpHeight / halfDuration;
        }

        private float ResolveJumpGravityScale()
        {
            var halfDuration = Mathf.Max(0.01f, _configData.PlayerJumpDuration * 0.5f);
            var gravityMagnitude = Mathf.Max(0.01f, -Physics2D.gravity.y);
            return ResolveJumpVelocity() / (gravityMagnitude * halfDuration);
        }

        private void PlayGameOverKnockback(Vector2 knockbackDirection)
        {
            if (_state == PlayerStateType.Knockback)
            {
                return;
            }

            _jumpElapsed = 0f;
            _rocketBoost.Reset();
            _knockback.Reset();
            _previousPosition = transform.position;
            ResetLandingSink();
            ChangeState(PlayerStateType.Knockback);

            var directionX = ResolveKnockbackDirectionX(knockbackDirection);
            ApplyFacingScale(directionX);
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.gravityScale = Mathf.Max(0f, _configData.PlayerGameOverKnockbackGravityScale);
            _rigidbody.linearVelocity = new Vector2(
                directionX * Mathf.Max(0f, _configData.PlayerGameOverKnockbackHorizontalSpeed),
                Mathf.Max(0f, _configData.PlayerGameOverKnockbackUpwardSpeed));
        }

        private void FreezeGameOverKnockbackIfDescending()
        {
            if (_knockback.IsFreezeLocked || _rigidbody.linearVelocity.y > 0f)
            {
                return;
            }

            _knockback.Freeze();
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.gravityScale = 0f;
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

        private Vector3 ResolveGroundContactPosition(Vector3 groundPosition)
        {
            groundPosition.y -= GroundContactOffset;
            return groundPosition;
        }

        private void BeginLandingSink(Vector3 basePosition)
        {
            _landingSink.Begin(basePosition);
            ApplyLandingSink(0f);
        }

        private void ResetLandingSink()
        {
            _landingSink.Reset(_groundPosition);
            PlaceRigidbody(_landingSink.BasePosition);
        }

        private void ReturnToShieldBlockStartPosition()
        {
            var startPosition = _groundPosition;
            _jumpElapsed = 0f;
            _rocketBoost.Reset();
            _knockback.Reset();
            _groundPosition = startPosition;
            _previousPosition = startPosition;
            PlaceRigidbody(startPosition);
            ResetLandingSink();
            ChangeState(PlayerStateType.Idle);
        }

        private void UpdateShieldBreakAnimation(float deltaTime)
        {
            _shield.TickBreakAnimation(deltaTime, _shieldVisualRoot, _shieldSpriteRenderer);
        }

        private void UpdateLandingSink(float deltaTime)
        {
            if (!_landingSink.Advance(deltaTime, _configData.PlayerLandingSinkDuration, out var normalizedTime))
            {
                return;
            }

            ApplyLandingSink(normalizedTime);
        }

        private void ApplyLandingSink(float normalizedTime)
        {
            const float DownPhase = 0.35f;
            var sinkProgress = normalizedTime < DownPhase
                ? Mathf.SmoothStep(0f, 1f, normalizedTime / DownPhase)
                : Mathf.SmoothStep(1f, 0f, (normalizedTime - DownPhase) / (1f - DownPhase));
            var position = _landingSink.BasePosition;
            position.y -= Mathf.Max(0f, _configData.PlayerLandingSinkOffset) * sinkProgress;
            PlaceRigidbody(position);
        }

        private void UpdateRocketBoost(float deltaTime)
        {
            var isComplete = _rocketBoost.Tick(
                deltaTime,
                _configData.PlayerRocketBoostDuration,
                out var nextPosition);
            _previousPosition = transform.position;
            _groundPosition = nextPosition;
            PlaceRigidbody(nextPosition);

            if (!isComplete)
            {
                return;
            }

            var targetPlatform = _rocketBoost.TargetPlatform;
            var targetGroundPosition = _rocketBoost.LandingGroundPosition;
            _rocketBoost.Reset();

            if (targetPlatform == null)
            {
                PlaceAtGroundPosition(targetGroundPosition, true);
                return;
            }

            StartRocketDrop();
        }

        private void StartRocketDrop()
        {
            _jumpElapsed = Mathf.Max(0.01f, _configData.PlayerJumpDuration);
            _previousPosition = transform.position;
            _groundPosition = transform.position;
            ResetLandingSink();
            ApplyRocketDropVelocity();
            ChangeState(PlayerStateType.Jump);
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
            if (_state == PlayerStateType.RocketBoost)
            {
                UpdateRocketBoost(Time.fixedDeltaTime);
                return;
            }

            if (_state == PlayerStateType.Knockback)
            {
                FreezeGameOverKnockbackIfDescending();
                return;
            }

            if (_state != PlayerStateType.Jump)
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
            _shieldIdleAnimatorStateHash = Animator.StringToHash("Empty");
            _shieldBreakAnimatorStateHash = Animator.StringToHash("Destory");
            _shield.CacheIdleSprite(_shieldSpriteRenderer.sprite);
            _defaultLocalScale = transform.localScale;
            _spawnPosition = transform.position;
            _groundPosition = _spawnPosition;
            _landingSink.Reset(_spawnPosition);
            _rocketBoost.Reset();
            _knockback.Reset();
            _previousPosition = _spawnPosition;
            _state = PlayerStateType.Idle;
            _shield.Reset(_shieldVisualRoot, _shieldSpriteRenderer);
            ApplyAnimatorState();
        }

        private void LateUpdate()
        {
            UpdateLandingSink(Time.deltaTime);
            UpdateShieldBreakAnimation(Time.deltaTime);
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
            if (!IsJumping || !IsDescending)
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
            if (!IsJumping || !IsDescending || other == null)
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
