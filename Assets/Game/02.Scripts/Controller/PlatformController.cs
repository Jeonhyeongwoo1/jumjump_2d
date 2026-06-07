using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;

namespace JumJump.Controller
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
    public sealed class PlatformController : MonoBehaviour
    {
        public float CenterY => transform.position.y;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Animator _animator;

        private float _gimmickTimerDuration;
        private float _gimmickTimerElapsed;
        private bool _isGimmickTimerRunning;
        private bool _isResolved;
        private bool _isActive;
        private bool _isInteractionEnabled;
        private bool _shouldTickGimmick;
        private Action<PlatformController> _onReleaseAction;
        private IPlatformGimmickBehaviour _gimmickBehaviour;
        private PlatformVisual _visual;
        private PlatformMotion _motion;
        private IEventBus _eventBus;
        private PlayerRegistry _playerRegistry;
        private GameConfigData _configData;

        public void Bind(
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            GameConfigData configData,
            UnityEngine.Camera gameCamera)
        {
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _configData = configData;
            if (_eventBus == null || _playerRegistry == null || _configData == null)
            {
                Debug.LogError($"[{nameof(PlatformController)}] Missing required dependency.");
                enabled = false;
                return;
            }

            _visual.BindCamera(gameCamera);
            _visual.CacheBaseSize(_configData);
        }

        public void InjectRelease(Action<PlatformController> onReleaseAction)
        {
            _onReleaseAction = onReleaseAction;
        }

        public void Initialize(
            Vector3 position,
            float targetX,
            float moveSpeed,
            IPlatformGimmickBehaviour gimmickBehaviour,
            PlatformGimmickSetting gimmickSetting)
        {
            _motion.Configure(position.x, targetX, moveSpeed);
            _gimmickBehaviour = gimmickBehaviour;
            _visual.CacheBaseSize(_configData);
            ResetForSpawn();
            PlaceRigidbody(position);
            _gimmickBehaviour?.Reset(this);
            _gimmickBehaviour?.Apply(this, gimmickSetting);
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

            var platformPosition = transform.position;
            var halfWidth = _visual.ResolveLandingHalfWidth();
            if (!PlatformLandingResolver.IsPlayerWithinContact(player, platformPosition, halfWidth, _configData))
            {
                return false;
            }

            var landingY = GetLandingSurfaceY();
            if (PlatformLandingResolver.CanResolveLanding(player, landingY))
            {
                ResolveLanding(player, landingY);
                return true;
            }

            if (PlatformLandingResolver.ShouldResolveSideHit(player, landingY))
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
            if (!PlatformLandingResolver.CanResolveStackedLanding(player, landingY, _configData))
            {
                return false;
            }

            ResolveStackedLanding(player, landingY);
            return true;
        }

        public bool IsLandingPointInside(Vector3 characterPosition, float characterVerticalOffset, float verticalTolerance)
        {
            return PlatformLandingResolver.IsLandingPointInside(
                transform.position,
                _visual.ResolveLandingHalfWidth(),
                characterPosition,
                characterVerticalOffset,
                verticalTolerance);
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
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_landingCollider == null)
            {
                _landingCollider = GetComponent<BoxCollider2D>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_spriteRenderer == null || _landingCollider == null || _rigidbody == null || _animator == null)
            {
                Debug.LogError($"[{nameof(PlatformController)}] Missing required component.");
                enabled = false;
                return;
            }

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.gravityScale = 0f;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            _visual.BindComponents(_spriteRenderer, _landingCollider, _animator);
        }

        private void FixedUpdate()
        {
            if (!_isActive || _isResolved)
            {
                return;
            }

            _motion.MoveTowardTarget(transform, _rigidbody, Time.fixedDeltaTime);
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

        internal void ApplyWidthScale(float widthScale)
        {
            _visual.ApplyScale(Mathf.Max(0.01f, widthScale), 1f);
        }

        internal void ApplyMoveSpeedScale(float moveSpeedScale)
        {
            _motion.ApplySpeedScale(moveSpeedScale);
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
        internal bool HasReachedMoveTarget => _motion.HasReachedTarget(transform.position.x);

        internal void SetInteractionEnabled(bool isEnabled)
        {
            _isInteractionEnabled = isEnabled;
            _visual.SetLandingColliderEnabled(isEnabled);
        }

        internal void EnterPreview(float alpha)
        {
            _motion.Pause();
            SetInteractionEnabled(true);
            SetPlatformAlpha(alpha);
        }

        internal void ActivateFromPreview()
        {
            SetPlatformAlpha(1f);
            SetInteractionEnabled(true);
            _motion.Resume();
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
            _visual.SetAlpha(alpha);
        }

        internal bool IsFullyInGameCameraView()
        {
            return _visual.IsFullyInGameCameraView();
        }

        internal bool IsMostlyInGameCameraView(float visibleRatio)
        {
            return _visual.IsMostlyInGameCameraView(visibleRatio);
        }

        private void ResetForSpawn()
        {
            _isResolved = false;
            _isActive = true;
            _isInteractionEnabled = true;
            ResetGimmickRuntime();
            SetPlatformAlpha(1f);
            _visual.ApplyScale(1f, 1f);
            _visual.SetLandingColliderEnabled(true);
            _visual.SetLandingColliderTrigger(true);
        }

        private void ResetGimmickRuntime()
        {
            _gimmickTimerDuration = 0f;
            _gimmickTimerElapsed = 0f;
            _isGimmickTimerRunning = false;
            _shouldTickGimmick = false;
        }

        private void PlaceRigidbody(Vector3 position)
        {
            _rigidbody.position = position;
            transform.position = position;
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

            var landingY = GetLandingSurfaceY();
            if (PlatformLandingResolver.ShouldResolveMissedPlayer(
                    player,
                    transform.position,
                    _visual.ResolveLandingHalfWidth(),
                    landingY,
                    _configData))
            {
                ResolveSideHit(player);
            }
        }

        private float GetLandingSurfaceY()
        {
            return _visual.GetLandingSurfaceY(_configData.PlatformLandingHeight);
        }

        private float ResolveStackHeight()
        {
            return _visual.ResolveStackHeight(_configData.PlatformHeight);
        }

        private void ResolveLanding(Player player, float landingY)
        {
            _isResolved = true;
            _motion.Stop();
            _gimmickBehaviour?.OnLanding(this);
            SetLandingColliderTrigger(false);
            PlayJumpAnimation();

            var landingPosition = player.Position;
            landingPosition.y = landingY + player.GroundContactOffset;
            player.LandOnPlatform(this, landingPosition);
        }

        private void ResolveStackedLanding(Player player, float landingY)
        {
            var landingPosition = player.Position;
            landingPosition.y = landingY + player.GroundContactOffset;
            _gimmickBehaviour?.OnLanding(this);
            PlayJumpAnimation();
            player.LandOnStackedPlatform(this, landingPosition);
        }

        private void PlayJumpAnimation()
        {
            _visual.PlayJumpAnimation();
        }

        private void ResolveSideHit(Player player)
        {
            _isResolved = true;
            _motion.Stop();
            StopGimmickTick();
            var knockbackDirection = PlatformLandingResolver.ResolveKnockbackDirection(
                player,
                transform.position,
                _motion.MoveDirectionX);
            _eventBus.Publish(new PlayerMissedLandingEvent(knockbackDirection));
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
            return PlatformLandingResolver.ResolvePlayerFromCollider(other, _playerRegistry.Player);
        }

        private void SetLandingColliderTrigger(bool isTrigger)
        {
            _visual.SetLandingColliderTrigger(isTrigger);
        }
    }
}
