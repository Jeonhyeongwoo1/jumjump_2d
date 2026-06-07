using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Controller
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public sealed class PlatformController : MonoBehaviour
    {
        public int PlatformIndex => _platformIndex;
        public PlatformGimmickType GimmickType => _gimmickType;
        public float CenterY => transform.position.y;



        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Animator _animator;

        private int _platformIndex;
        private PlatformGimmickType _gimmickType;
        private int _jumpAnimationStateHash;
        private Vector3 _baseLocalScale;
        private Vector3 _baseSpriteLocalScale;
        private Color _baseSpriteColor;
        private Vector2 _baseSpriteSize;
        private Vector2 _baseColliderSize;
        private float _halfWidth;
        private float _landingHeight;
        private float _targetX;
        private float _moveSpeed;
        private float _pausedMoveSpeed;
        private float _moveDirectionX;
        private float _gimmickTimerDuration;
        private float _gimmickTimerElapsed;
        private bool _isGimmickTimerRunning;
        private bool _isResolved;
        private bool _isActive;
        private bool _isInteractionEnabled;
        private bool _hasPausedMoveSpeed;
        private bool _hasCachedBaseSize;
        private bool _shouldTickGimmick;
        private Action<PlatformController> _onReleaseAction;
        private IPlatformGimmickBehaviour _gimmickBehaviour;
        private IEventBus _eventBus;
        private PlayerRegistry _playerRegistry;
        private GameConfigData _configData;
        private UnityEngine.Camera _gameCamera;

        public void Bind(
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            GameConfigData configData,
            UnityEngine.Camera gameCamera)
        {
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _configData = configData;
            _gameCamera = gameCamera;
            if (_eventBus == null || _playerRegistry == null || _configData == null)
            {
                Debug.LogError($"[{nameof(PlatformController)}] Missing required dependency.");
                enabled = false;
                return;
            }

            CacheBaseSize();
        }

        public void InjectRelease(Action<PlatformController> onReleaseAction)
        {
            _onReleaseAction = onReleaseAction;
        }

        public void Initialize(
            int platformIndex,
            PlatformGimmickType gimmickType,
            Vector3 position,
            float targetX,
            float landingHeight,
            float moveSpeed,
            IPlatformGimmickBehaviour gimmickBehaviour,
            PlatformGimmickSetting gimmickSetting)
        {
            _platformIndex = platformIndex;
            _gimmickType = gimmickType;
            _landingHeight = Mathf.Max(0.01f, landingHeight);
            _targetX = targetX;
            _moveSpeed = moveSpeed;
            _moveDirectionX = ResolveMoveDirectionX(position.x, targetX);
            _gimmickBehaviour = gimmickBehaviour;
            CacheBaseSize();
            ResetForSpawn();
            PlaceRigidbody(position);
            _gimmickBehaviour?.Reset(this);
            _gimmickBehaviour?.Apply(this, gimmickSetting);
            _halfWidth = ResolveLandingHalfWidth();
            gameObject.SetActive(true);
        }

        public Vector3 GetLandingPosition(float characterVerticalOffset)
        {
            var platformPosition = transform.position;
            return new Vector3(platformPosition.x, platformPosition.y + characterVerticalOffset, platformPosition.z);
        }

        public Vector3 GetLandingPosition(Player player)
        {
            var platformPosition = transform.position;
            var groundContactOffset = player == null ? 0f : player.GroundContactOffset;
            return new Vector3(platformPosition.x, GetLandingSurfaceY() + groundContactOffset, platformPosition.z);
        }

        public float GetStackedNextCenterY()
        {
            return transform.position.y + ResolveStackHeight() + _configData.PlatformStackVerticalOffset;
        }

        public bool TryResolveLanding(Player player)
        {
            if (!_isActive || !_isInteractionEnabled || _isResolved || player == null)
            {
                return false;
            }

            var playerPosition = player.Position;
            var contactHalfWidth = Mathf.Max(0f, _halfWidth + _configData.PlayerContactHalfWidth);
            if (Mathf.Abs(playerPosition.x - transform.position.x) > contactHalfWidth)
            {
                return false;
            }

            var landingY = GetLandingSurfaceY();
            if (player.CanLand && player.IsDescending && HasCrossedLandingSurface(player, landingY))
            {
                ResolveLanding(player, landingY);
                return true;
            }

            if (player.BottomY < landingY)
            {
                ResolveSideHit(player);
            }

            return false;
        }

        public bool TryResolveStackedLanding(Player player)
        {
            if (!_isActive || !_isInteractionEnabled || !_isResolved || player == null ||
                !player.CanLand || !player.IsDescending)
            {
                return false;
            }

            var landingY = GetLandingSurfaceY();
            if (!HasCrossedLandingSurface(player, landingY) && !IsTouchingLandingSurface(player, landingY))
            {
                return false;
            }

            var landingPosition = player.Position;
            landingPosition.y = landingY + player.GroundContactOffset;
            _gimmickBehaviour?.OnLanding(this);
            PlayJumpAnimation();
            player.LandOnStackedPlatform(this, landingPosition);
            return true;
        }

        public bool IsLandingPointInside(Vector3 characterPosition, float characterVerticalOffset, float verticalTolerance)
        {
            var platformPosition = transform.position;
            var landingY = platformPosition.y + characterVerticalOffset;

            if (Mathf.Abs(characterPosition.y - landingY) > verticalTolerance)
            {
                return false;
            }

            return characterPosition.x >= platformPosition.x - _halfWidth &&
                   characterPosition.x <= platformPosition.x + _halfWidth;
        }

        public void Release()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _gimmickBehaviour?.Reset(this);
            ResetGimmickRuntime();
            gameObject.SetActive(false);
            _onReleaseAction?.Invoke(this);
        }

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_landingCollider == null)
            {
                _landingCollider = GetComponent<BoxCollider2D>();
            }

            if (_rigidbody == null && !TryGetComponent(out _rigidbody))
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_rigidbody != null)
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
                _rigidbody.gravityScale = 0f;
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (_animator == null)
            {
                Debug.LogError($"[{nameof(PlatformController)}] Missing required component: {nameof(_animator)}.");
                enabled = false;
                return;
            }

            _jumpAnimationStateHash = Animator.StringToHash("JumpAnimation");
        }

        private void FixedUpdate()
        {
            if (!_isActive || _isResolved)
            {
                return;
            }

            MoveTowardTarget();
            EvaluateMissedPlayer();
        }

        private void Update()
        {
            if (!_isActive || _isResolved || !_shouldTickGimmick || _gimmickBehaviour == null)
            {
                return;
            }

            _gimmickBehaviour.Tick(this, Time.deltaTime);
        }

        private void MoveTowardTarget()
        {
            if (_moveSpeed <= 0f)
            {
                return;
            }

            var nextPosition = transform.position;
            nextPosition.x = Mathf.MoveTowards(nextPosition.x, _targetX, _moveSpeed * Time.fixedDeltaTime);
            MoveRigidbody(nextPosition);
        }

        internal void ApplyWidthScale(float widthScale)
        {
            ApplyPlatformScale(Mathf.Max(0.01f, widthScale), 1f);
        }

        internal void ApplyMoveSpeedScale(float moveSpeedScale)
        {
            _moveSpeed *= Mathf.Max(0f, moveSpeedScale);
        }

        internal void StartGimmickTimer(float duration)
        {
            PrepareGimmickTimer(duration);
            StartPreparedGimmickTimer();
        }

        internal void PrepareGimmickTimer(float duration)
        {
            _gimmickTimerDuration = Mathf.Max(0f, duration);
            _gimmickTimerElapsed = 0f;
            _isGimmickTimerRunning = false;
        }

        internal void StartPreparedGimmickTimer()
        {
            _gimmickTimerElapsed = 0f;
            _isGimmickTimerRunning = true;
            _shouldTickGimmick = _gimmickBehaviour != null && _gimmickBehaviour.RequiresTick;
        }

        internal void EnableGimmickTick()
        {
            _shouldTickGimmick = _gimmickBehaviour != null && _gimmickBehaviour.RequiresTick;
        }

        internal float AdvanceGimmickTimer(float deltaTime)
        {
            if (_gimmickTimerDuration <= 0f)
            {
                return 1f;
            }

            _gimmickTimerElapsed = Mathf.Min(_gimmickTimerDuration, _gimmickTimerElapsed + Mathf.Max(0f, deltaTime));
            return Mathf.Clamp01(_gimmickTimerElapsed / _gimmickTimerDuration);
        }

        internal bool IsGimmickTimerComplete => _gimmickTimerDuration <= 0f || _gimmickTimerElapsed >= _gimmickTimerDuration;
        internal bool IsGimmickTimerRunning => _isGimmickTimerRunning;
        internal bool HasReachedMoveTarget => Mathf.Abs(transform.position.x - _targetX) <= GameConst.Platform.MoveTargetEpsilon;

        internal void SetInteractionEnabled(bool isEnabled)
        {
            _isInteractionEnabled = isEnabled;
            if (_landingCollider != null)
            {
                _landingCollider.enabled = isEnabled;
            }
        }

        internal void EnterPreview(float alpha)
        {
            PauseMovement();
            SetInteractionEnabled(true);
            SetPlatformAlpha(alpha);
        }

        internal void ActivateFromPreview()
        {
            SetPlatformAlpha(1f);
            SetInteractionEnabled(true);
            ResumeMovement();
        }

        internal void StopGimmickTick()
        {
            _shouldTickGimmick = false;
            _isGimmickTimerRunning = false;
        }

        internal void RevealPlatformVisual()
        {
            ResetGimmickRuntime();
            SetPlatformAlpha(1f);
        }

        internal void SetPlatformAlpha(float alpha)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            var color = _baseSpriteColor;
            color.a *= Mathf.Clamp01(alpha);
            _spriteRenderer.color = color;
        }

        internal bool IsFullyInGameCameraView()
        {
            if (_gameCamera == null || _spriteRenderer == null)
            {
                return false;
            }

            var bounds = _spriteRenderer.bounds;
            var viewportMin = _gameCamera.WorldToViewportPoint(bounds.min);
            var viewportMax = _gameCamera.WorldToViewportPoint(bounds.max);

            if (viewportMin.z < 0f || viewportMax.z < 0f)
            {
                return false;
            }

            return viewportMin.x >= 0f &&
                   viewportMax.x <= 1f &&
                   viewportMin.y >= 0f &&
                   viewportMax.y <= 1f;
        }

        internal bool IsMostlyInGameCameraView(float visibleRatio)
        {
            if (_gameCamera == null || _spriteRenderer == null)
            {
                return false;
            }

            var bounds = _spriteRenderer.bounds;
            var viewportMin = _gameCamera.WorldToViewportPoint(bounds.min);
            var viewportMax = _gameCamera.WorldToViewportPoint(bounds.max);

            if (viewportMin.z < 0f || viewportMax.z < 0f)
            {
                return false;
            }

            if (viewportMax.y < 0f || viewportMin.y > 1f)
            {
                return false;
            }

            var width = Mathf.Max(0.0001f, viewportMax.x - viewportMin.x);
            var visibleMinX = Mathf.Clamp01(viewportMin.x);
            var visibleMaxX = Mathf.Clamp01(viewportMax.x);
            var visibleWidth = Mathf.Max(0f, visibleMaxX - visibleMinX);
            return visibleWidth / width >= Mathf.Clamp01(visibleRatio);
        }

        private void PlaceRigidbody(Vector3 position)
        {
            if (_rigidbody == null)
            {
                transform.position = position;
                return;
            }

            _rigidbody.position = position;
            transform.position = position;
        }

        private void ResetForSpawn()
        {
            _isResolved = false;
            _isActive = true;
            _isInteractionEnabled = true;
            _hasPausedMoveSpeed = false;
            _pausedMoveSpeed = 0f;
            ResetGimmickRuntime();
            SetPlatformAlpha(1f);
            ApplyPlatformScale(1f, 1f);
            if (_landingCollider != null)
            {
                _landingCollider.enabled = true;
            }

            SetLandingColliderTrigger(true);
        }

        private void ResetGimmickRuntime()
        {
            _gimmickTimerDuration = 0f;
            _gimmickTimerElapsed = 0f;
            _isGimmickTimerRunning = false;
            _shouldTickGimmick = false;
        }

        private void CacheBaseSize()
        {
            if (_hasCachedBaseSize)
            {
                return;
            }

            _baseLocalScale = transform.localScale;
            _baseSpriteLocalScale = _spriteRenderer.transform.localScale;
            _baseSpriteColor = _spriteRenderer.color;
            _baseSpriteSize = _spriteRenderer.size;
            _baseColliderSize = ResolveBaseColliderSize();
            _hasCachedBaseSize = true;
        }

        private Vector2 ResolveBaseColliderSize()
        {
            var colliderSize = _landingCollider.size;
            if (colliderSize.x < GameConst.Platform.MinimumColliderDimension)
            {
                colliderSize.x =  Mathf.Max(GameConst.Platform.MinimumColliderDimension, _configData.PlatformWidth);
            }

            if (colliderSize.y < GameConst.Platform.MinimumColliderDimension)
            {
                colliderSize.y = Mathf.Max(GameConst.Platform.MinimumColliderDimension, _configData.PlatformLandingHeight);
            }

            return colliderSize;
        }

        private void ApplyPlatformScale(float widthScale, float heightScale)
        {
            var spriteOnRoot = _spriteRenderer != null && _spriteRenderer.transform == transform;
            transform.localScale = spriteOnRoot
                ? new Vector3(_baseLocalScale.x * widthScale, _baseLocalScale.y * heightScale, _baseLocalScale.z)
                : _baseLocalScale;

            if (_spriteRenderer != null)
            {
                if (_spriteRenderer.drawMode == SpriteDrawMode.Sliced ||
                    _spriteRenderer.drawMode == SpriteDrawMode.Tiled)
                {
                    _spriteRenderer.size = spriteOnRoot
                        ? _baseSpriteSize
                        : new Vector2(
                            _baseSpriteSize.x * widthScale,
                            _baseSpriteSize.y * heightScale);
                    if (!spriteOnRoot)
                    {
                        _spriteRenderer.transform.localScale = _baseSpriteLocalScale;
                    }
                }
                else if (!spriteOnRoot)
                {
                    _spriteRenderer.transform.localScale = new Vector3(
                        _baseSpriteLocalScale.x * widthScale,
                        _baseSpriteLocalScale.y * heightScale,
                        _baseSpriteLocalScale.z);
                }
            }

            if (_landingCollider != null)
            {
                var colliderWidthScale = spriteOnRoot ? 1f : widthScale;
                var colliderHeightScale = spriteOnRoot ? 1f : heightScale;
                _landingCollider.size = new Vector2(
                    _baseColliderSize.x * colliderWidthScale,
                    _baseColliderSize.y * colliderHeightScale);
                _landingCollider.isTrigger = true;
            }
        }

        private float ResolveLandingHalfWidth()
        {
            if (_landingCollider != null)
            {
                return Mathf.Max(0.01f, _landingCollider.bounds.size.x * 0.5f);
            }

            if (_spriteRenderer != null)
            {
                return Mathf.Max(0.01f, _spriteRenderer.bounds.size.x * 0.5f);
            }

            return Mathf.Max(0.01f, _configData.PlatformWidth * 0.5f);
        }

        private void MoveRigidbody(Vector3 position)
        {
            if (_rigidbody == null)
            {
                transform.position = position;
                return;
            }

            _rigidbody.MovePosition(position);
        }

        private void PauseMovement()
        {
            if (_hasPausedMoveSpeed)
            {
                return;
            }

            _pausedMoveSpeed = _moveSpeed;
            _moveSpeed = 0f;
            _hasPausedMoveSpeed = true;
        }

        private void ResumeMovement()
        {
            if (!_hasPausedMoveSpeed)
            {
                return;
            }

            _moveSpeed = _pausedMoveSpeed;
            _pausedMoveSpeed = 0f;
            _hasPausedMoveSpeed = false;
        }

        private void EvaluateMissedPlayer()
        {
            if (!_isInteractionEnabled)
            {
                return;
            }

            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            var playerPosition = player.Position;
            var contactHalfWidth = Mathf.Max(0f, _halfWidth + _configData.PlayerContactHalfWidth);
            if (Mathf.Abs(playerPosition.x - transform.position.x) > contactHalfWidth)
            {
                return;
            }

            var landingY = GetLandingSurfaceY();

            if (player.IsJumping &&
                player.IsDescending &&
                player.BottomY < landingY - Mathf.Max(0f, _configData.PlatformSideHitTopMargin))
            {
                ResolveSideHit(player);
            }
        }

        private float GetLandingSurfaceY()
        {
            if (_landingCollider != null)
            {
                return _landingCollider.bounds.max.y;
            }

            return transform.position.y + _landingHeight * 0.5f;
        }

        private float ResolveStackHeight()
        {
            if (_landingCollider != null)
            {
                return Mathf.Max(0f, _landingCollider.bounds.size.y);
            }

            return Mathf.Max(0f, _configData.PlatformHeight);
        }

        private bool HasCrossedLandingSurface(Player player, float landingY)
        {
            return player.PreviousBottomY >= landingY && player.BottomY <= landingY;
        }

        private bool IsTouchingLandingSurface(Player player, float landingY)
        {
            var snapTolerance = Mathf.Min(0.08f, Mathf.Max(0.01f, _configData.PlayerLandingVerticalTolerance));
            return player.BottomY <= landingY + snapTolerance && player.Position.y >= landingY;
        }

        private void ResolveLanding(Player player, float landingY)
        {
            _isResolved = true;
            _moveSpeed = 0f;
            _gimmickBehaviour?.OnLanding(this);
            SetLandingColliderTrigger(false);
            PlayJumpAnimation();

            var landingPosition = player.Position;
            landingPosition.y = landingY + player.GroundContactOffset;
            player.LandOnPlatform(this, landingPosition);
        }

        private void PlayJumpAnimation()
        {
            _animator.Play(_jumpAnimationStateHash, 0, 0f);
        }

        private void ResolveSideHit(Player player)
        {
            _isResolved = true;
            _moveSpeed = 0f;
            StopGimmickTick();
            _eventBus.Publish(new PlayerMissedLandingEvent(ResolveKnockbackDirection(player)));
        }

        private Vector2 ResolveKnockbackDirection(Player player)
        {
            var directionX = _moveDirectionX;
            if (Mathf.Approximately(directionX, 0f) && player != null)
            {
                directionX = Mathf.Sign(player.Position.x - transform.position.x);
            }

            if (Mathf.Approximately(directionX, 0f))
            {
                directionX = 1f;
            }

            return new Vector2(directionX, 0f);
        }

        private float ResolveMoveDirectionX(float spawnX, float targetX)
        {
            var deltaX = targetX - spawnX;
            if (Mathf.Approximately(deltaX, 0f))
            {
                return 0f;
            }

            return Mathf.Sign(deltaX);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryResolvePlayerTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryResolvePlayerTrigger(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryResolvePlayerCollision(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryResolvePlayerCollision(collision.collider);
        }

        private void TryResolvePlayerTrigger(Collider2D other)
        {
            var player = ResolvePlayerFromCollider(other);
            if (player == null)
            {
                return;
            }

            TryResolveLanding(player);
        }

        private void TryResolvePlayerCollision(Collider2D other)
        {
            var player = ResolvePlayerFromCollider(other);
            if (player == null)
            {
                return;
            }

            TryResolveStackedLanding(player);
        }

        private Player ResolvePlayerFromCollider(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            var player = _playerRegistry.Player;
            if (player == null)
            {
                return null;
            }

            if (other.attachedRigidbody != null && other.attachedRigidbody.transform == player.transform)
            {
                return player;
            }

            return other.transform == player.transform || other.transform.IsChildOf(player.transform) ? player : null;
        }

        private void SetLandingColliderTrigger(bool isTrigger)
        {
            if (_landingCollider == null)
            {
                return;
            }

            _landingCollider.isTrigger = isTrigger;
        }
    }
}
