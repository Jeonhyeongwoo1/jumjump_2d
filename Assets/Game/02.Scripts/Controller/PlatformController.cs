using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class PlatformController : MonoBehaviour
    {
        public int PlatformIndex => _platformIndex;
        public float CenterY => transform.position.y;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;

        private int _platformIndex;
        private float _halfWidth;
        private float _landingHeight;
        private float _targetX;
        private float _moveSpeed;
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
            _isResolved = false;
            _isActive = true;
            transform.position = position;
            transform.localScale = new Vector3(width, height, 1f);
            gameObject.SetActive(true);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.size = new Vector2(width, height);
            }

            if (_landingCollider != null)
            {
                _landingCollider.size = new Vector2(width, _landingHeight);
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
            return transform.position.y + ResolveStackHeight();
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
        }

        private void Update()
        {
            if (!_isActive || _isResolved)
            {
                return;
            }

            MoveTowardTarget();
            EvaluatePlayerContact();
        }

        private void MoveTowardTarget()
        {
            if (_moveSpeed <= 0f)
            {
                return;
            }

            var nextPosition = transform.position;
            nextPosition.x = Mathf.MoveTowards(nextPosition.x, _targetX, _moveSpeed * Time.deltaTime);
            transform.position = nextPosition;
        }

        private void EvaluatePlayerContact()
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

            if (player.CanLand && player.IsDescending && HasCrossedLandingLine(player, landingY))
            {
                ResolveLanding(player, landingY);
                return;
            }

            if (player.IsJumping &&
                player.IsDescending &&
                player.BottomY < landingY - Mathf.Max(0f, _configData.PlatformSideHitTopMargin))
            {
                ResolveSideHit();
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
            if (_spriteRenderer != null)
            {
                return Mathf.Max(0f, _spriteRenderer.bounds.size.y);
            }

            if (_landingCollider != null)
            {
                return Mathf.Max(0f, _landingCollider.bounds.size.y);
            }

            return _configData == null ? 0f : Mathf.Max(0f, _configData.PlatformHeight);
        }

        private bool HasCrossedLandingLine(Player player, float landingY)
        {
            return player.PreviousBottomY >= landingY && player.BottomY <= landingY;
        }

        private void ResolveLanding(Player player, float landingY)
        {
            _isResolved = true;
            _moveSpeed = 0f;

            var landingPosition = player.Position;
            landingPosition.y = landingY + player.GroundContactOffset;
            player.LandOnPlatform(this, landingPosition);
        }

        private void ResolveSideHit()
        {
            _isResolved = true;
            _moveSpeed = 0f;
            _eventBus.Publish(new PlayerMissedLandingEvent());
        }
    }
}
