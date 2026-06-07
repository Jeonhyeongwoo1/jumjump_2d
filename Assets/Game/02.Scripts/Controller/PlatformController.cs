using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;

namespace JumJump.Controller
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlatformController : MonoBehaviour
    {
        public int PlatformIndex => _platformIndex;
        public PlatformGimmickType GimmickType => _gimmickType;
        public float CenterY => transform.position.y;

        private const float MinimumColliderDimension = 0.01f;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private Rigidbody2D _rigidbody;

        private int _platformIndex;
        private PlatformGimmickType _gimmickType;
        private Vector3 _baseLocalScale;
        private Vector3 _baseSpriteLocalScale;
        private Vector2 _baseSpriteSize;
        private Vector2 _baseColliderSize;
        private float _halfWidth;
        private float _landingHeight;
        private float _targetX;
        private float _moveSpeed;
        private float _moveDirectionX;
        private bool _isResolved;
        private bool _isActive;
        private bool _hasCachedBaseSize;
        private Action<PlatformController> _onReleaseAction;
        private IEventBus _eventBus;
        private PlayerRegistry _playerRegistry;
        private GameConfigData _configData;

        public void Bind(IEventBus eventBus, PlayerRegistry playerRegistry, GameConfigData configData)
        {
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _configData = configData;
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
            float widthScale,
            float heightScale,
            float landingHeight,
            float moveSpeed)
        {
            var safeWidthScale = Mathf.Max(0.01f, widthScale);
            var safeHeightScale = Mathf.Max(0.01f, heightScale);

            _platformIndex = platformIndex;
            _gimmickType = gimmickType;
            _landingHeight = Mathf.Max(0.01f, landingHeight);
            _targetX = targetX;
            _moveSpeed = moveSpeed;
            _moveDirectionX = ResolveMoveDirectionX(position.x, targetX);
            _isResolved = false;
            _isActive = true;
            CacheBaseSize();
            PlaceRigidbody(position);
            ApplyPlatformScale(safeWidthScale, safeHeightScale);
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
            var stackOffset = _configData == null ? 0f : _configData.PlatformStackVerticalOffset;
            return transform.position.y + ResolveStackHeight() + stackOffset;
        }

        public bool TryResolveLanding(Player player)
        {
            if (!_isActive || _isResolved || _configData == null || player == null)
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
            if (!_isActive || !_isResolved || _configData == null || player == null || !player.CanLand || !player.IsDescending)
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

            if (_rigidbody != null)
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
                _rigidbody.gravityScale = 0f;
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
                _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
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

        private void CacheBaseSize()
        {
            if (_hasCachedBaseSize)
            {
                return;
            }

            _baseLocalScale = transform.localScale;
            _baseSpriteLocalScale = _spriteRenderer.transform.localScale;
            _baseSpriteSize = _spriteRenderer.size;
            _baseColliderSize = ResolveBaseColliderSize();
            _hasCachedBaseSize = true;
        }

        private Vector2 ResolveBaseColliderSize()
        {
            var colliderSize = _landingCollider.size;
            if (colliderSize.x < MinimumColliderDimension)
            {
                colliderSize.x =  Mathf.Max(MinimumColliderDimension, _configData.PlatformWidth);
            }

            if (colliderSize.y < MinimumColliderDimension)
            {
                colliderSize.y = Mathf.Max(MinimumColliderDimension, _configData.PlatformLandingHeight);
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

            return _configData == null ? 0.01f : Mathf.Max(0.01f, _configData.PlatformWidth * 0.5f);
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

        private void EvaluateMissedPlayer()
        {
            if (_eventBus == null || _playerRegistry == null || _configData == null)
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

            return _configData == null ? 0f : Mathf.Max(0f, _configData.PlatformHeight);
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
            SetLandingColliderTrigger(false);

            var landingPosition = player.Position;
            landingPosition.y = landingY + player.GroundContactOffset;
            player.LandOnPlatform(this, landingPosition);
        }

        private void ResolveSideHit(Player player)
        {
            _isResolved = true;
            _moveSpeed = 0f;
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
            var player = _playerRegistry == null ? null : _playerRegistry.Player;
            if (player == null || other == null)
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
