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
        public float CenterY => transform.position.y;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private Rigidbody2D _rigidbody;

        private int _platformIndex;
        private float _halfWidth;
        private float _landingHeight;
        private float _targetX;
        private float _moveSpeed;
        private float _moveDirectionX;
        private bool _isResolved;
        private bool _isActive;
        private Action<PlatformController> _onReleaseAction;
        private IEventBus _eventBus;
        private PlayerRegistry _playerRegistry;
        private GameConfigData _configData;

        public void Bind(IEventBus eventBus, PlayerRegistry playerRegistry, GameConfigData configData)
        {
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _configData = configData;
        }

        public void InjectRelease(Action<PlatformController> onReleaseAction)
        {
            _onReleaseAction = onReleaseAction;
        }

        public void Initialize(
            int platformIndex,
            Vector3 position,
            float targetX,
            float width,
            float height,
            float landingHeight,
            float moveSpeed)
        {
            _platformIndex = platformIndex;
            _halfWidth = width * 0.5f;
            _landingHeight = landingHeight;
            _targetX = targetX;
            _moveSpeed = moveSpeed;
            _moveDirectionX = ResolveMoveDirectionX(position.x, targetX);
            _isResolved = false;
            _isActive = true;
            PlaceRigidbody(position);
            transform.localScale = new Vector3(width, height, 1f);
            gameObject.SetActive(true);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.size = new Vector2(width, height);
            }

            if (_landingCollider != null)
            {
                // _landingCollider.size = new Vector2(_landingCollider.size.x, _landingHeight);
                _landingCollider.isTrigger = true;
            }
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
