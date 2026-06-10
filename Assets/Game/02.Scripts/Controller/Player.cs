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
        [SerializeField] private GameObject _shieldVisualRoot;
        [SerializeField] private SpriteRenderer _shieldSpriteRenderer;
        [SerializeField] private Animator _shieldAnimator;

        private bool _isJumping;
        private bool _hasShield;
        private bool _isShieldBreakAnimating;
        private bool _isRocketBoosting;
        private bool _isGameOverKnockback;
        private bool _isGameOverFreezeLocked;
        private float _jumpElapsed;
        private float _shieldBreakElapsed;
        private float _rocketBoostElapsed;
        private int _isJumpingAnimatorParameterHash;
        private int _isDeadAnimatorParameterHash;
        private int _idleAnimatorStateHash;
        private int _shieldIdleAnimatorStateHash;
        private int _shieldBreakAnimatorStateHash;
        private PlayerStateType _state;
        private PlatformController _rocketTargetPlatform;
        private Vector3 _spawnPosition;
        private Vector3 _groundPosition;
        private Vector3 _previousPosition;
        private Vector3 _rocketBoostStartGroundPosition;
        private Vector3 _rocketBoostTargetGroundPosition;
        private Vector3 _defaultLocalScale;
        private Vector3 _landingSinkBasePosition;
        private Sprite _shieldIdleSprite;
        private float _landingSinkElapsed;
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
            _isJumping = false;
            _hasShield = false;
            _isShieldBreakAnimating = false;
            _isRocketBoosting = false;
            _isGameOverKnockback = false;
            _isGameOverFreezeLocked = false;
            _jumpElapsed = 0f;
            _shieldBreakElapsed = 0f;
            _rocketBoostElapsed = 0f;
            _rocketTargetPlatform = null;
            _groundPosition = _spawnPosition;
            _previousPosition = _groundPosition;
            ApplyFacingScale(1f);
            PlaceRigidbody(_groundPosition);
            ResetLandingSink();
            HideShieldVisual();
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
            _hasShield = true;
            ShowShieldVisual();
        }

        public bool TryBlockWithShield()
        {
            if (!_hasShield)
            {
                return false;
            }

            _hasShield = false;
            PlayShieldBreakAnimation();
            ReturnToShieldBlockStartPosition();
            return true;
        }

        public void StartRocketBoost(PlatformController targetPlatform, Vector3 targetGroundPosition)
        {
            _isJumping = false;
            _isRocketBoosting = true;
            _isGameOverKnockback = false;
            _isGameOverFreezeLocked = false;
            _jumpElapsed = 0f;
            _rocketBoostElapsed = 0f;
            _rocketTargetPlatform = targetPlatform;
            _rocketBoostStartGroundPosition = _groundPosition;
            _rocketBoostTargetGroundPosition = targetGroundPosition;
            _previousPosition = _rocketBoostStartGroundPosition;
            ResetLandingSink();
            ChangeState(PlayerStateType.Jump);
        }

        private void OnPlayerJumpRequested(in PlayerJumpRequestedEvent ev)
        {
            if (_isJumping || _isRocketBoosting || _isGameOverKnockback)
            {
                return;
            }

            _jumpElapsed = 0f;
            ResetLandingSink();
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

        private void PlaceAtGroundPosition(Vector3 groundPosition, bool playLandingSink)
        {
            _isJumping = false;
            _isRocketBoosting = false;
            _isGameOverKnockback = false;
            _isGameOverFreezeLocked = false;
            _jumpElapsed = 0f;
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
            _isRocketBoosting = false;
            _isGameOverKnockback = true;
            _isGameOverFreezeLocked = false;
            _jumpElapsed = 0f;
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
            if (_isGameOverFreezeLocked || _rigidbody.linearVelocity.y > 0f)
            {
                return;
            }

            _isGameOverFreezeLocked = true;
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
            _landingSinkBasePosition = basePosition;
            _landingSinkElapsed = 0f;
            ApplyLandingSink(0f);
        }

        private void ResetLandingSink()
        {
            _landingSinkElapsed = float.PositiveInfinity;
            _landingSinkBasePosition = _groundPosition;
            PlaceRigidbody(_landingSinkBasePosition);
        }

        private void ShowShieldVisual()
        {
            _isShieldBreakAnimating = false;
            _shieldBreakElapsed = 0f;
            _shieldSpriteRenderer.sprite = _shieldIdleSprite;
            _shieldVisualRoot.SetActive(true);
            _shieldAnimator.Play(_shieldIdleAnimatorStateHash, 0, 0f);
        }

        private void PlayShieldBreakAnimation()
        {
            _isShieldBreakAnimating = true;
            _shieldBreakElapsed = 0f;
            _shieldVisualRoot.SetActive(true);
            _shieldAnimator.Play(_shieldBreakAnimatorStateHash, 0, 0f);
        }

        private void ReturnToShieldBlockStartPosition()
        {
            var startPosition = _groundPosition;
            _isJumping = false;
            _isRocketBoosting = false;
            _isGameOverKnockback = false;
            _isGameOverFreezeLocked = false;
            _jumpElapsed = 0f;
            _rocketBoostElapsed = 0f;
            _rocketTargetPlatform = null;
            _groundPosition = startPosition;
            _previousPosition = startPosition;
            PlaceRigidbody(startPosition);
            ResetLandingSink();
            ChangeState(PlayerStateType.Idle);
        }

        private void HideShieldVisual()
        {
            _isShieldBreakAnimating = false;
            _shieldBreakElapsed = 0f;
            _shieldSpriteRenderer.sprite = _shieldIdleSprite;
            _shieldVisualRoot.SetActive(false);
        }

        private void UpdateShieldBreakAnimation(float deltaTime)
        {
            if (!_isShieldBreakAnimating)
            {
                return;
            }

            _shieldBreakElapsed += deltaTime;
            if (_shieldBreakElapsed >= GameConst.Player.ShieldBreakAnimationDuration)
            {
                HideShieldVisual();
            }
        }

        private void UpdateLandingSink(float deltaTime)
        {
            var duration = Mathf.Max(0.01f, _configData.PlayerLandingSinkDuration);
            if (_landingSinkElapsed >= duration)
            {
                return;
            }

            _landingSinkElapsed += deltaTime;
            var normalizedTime = Mathf.Clamp01(_landingSinkElapsed / duration);
            ApplyLandingSink(normalizedTime);
        }

        private void ApplyLandingSink(float normalizedTime)
        {
            const float DownPhase = 0.35f;
            var sinkProgress = normalizedTime < DownPhase
                ? Mathf.SmoothStep(0f, 1f, normalizedTime / DownPhase)
                : Mathf.SmoothStep(1f, 0f, (normalizedTime - DownPhase) / (1f - DownPhase));
            var position = _landingSinkBasePosition;
            position.y -= Mathf.Max(0f, _configData.PlayerLandingSinkOffset) * sinkProgress;
            PlaceRigidbody(position);
        }

        private void UpdateRocketBoost(float deltaTime)
        {
            var duration = Mathf.Max(0.01f, _configData.PlayerRocketBoostDuration);
            _rocketBoostElapsed += deltaTime;
            var normalizedTime = Mathf.Clamp01(_rocketBoostElapsed / duration);
            var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            var nextPosition = Vector3.Lerp(
                _rocketBoostStartGroundPosition,
                _rocketBoostTargetGroundPosition,
                easedTime);

            _previousPosition = transform.position;
            _groundPosition = nextPosition;
            PlaceRigidbody(nextPosition);

            if (normalizedTime < 1f)
            {
                return;
            }

            var targetPlatform = _rocketTargetPlatform;
            var targetGroundPosition = _rocketBoostTargetGroundPosition;
            _isRocketBoosting = false;
            _rocketTargetPlatform = null;

            if (targetPlatform == null)
            {
                PlaceAtGroundPosition(targetGroundPosition, true);
                return;
            }

            LandOnPlatform(targetPlatform, targetGroundPosition);
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
            if (_isRocketBoosting)
            {
                UpdateRocketBoost(Time.fixedDeltaTime);
                return;
            }

            if (_isGameOverKnockback)
            {
                FreezeGameOverKnockbackIfDescending();
                return;
            }

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
            _shieldIdleAnimatorStateHash = Animator.StringToHash("Empty");
            _shieldBreakAnimatorStateHash = Animator.StringToHash("Destory");
            _shieldIdleSprite = _shieldSpriteRenderer.sprite;
            _defaultLocalScale = transform.localScale;
            _spawnPosition = transform.position;
            _groundPosition = _spawnPosition;
            _landingSinkBasePosition = _spawnPosition;
            _landingSinkElapsed = float.PositiveInfinity;
            _previousPosition = _spawnPosition;
            _state = PlayerStateType.Idle;
            HideShieldVisual();
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
