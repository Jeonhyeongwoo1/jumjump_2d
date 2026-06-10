using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Controller
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
    public sealed class PlatformController : MonoBehaviour
    {
        public float CenterY => transform.position.y;
        public PlatformGimmickType GimmickType => _gimmickType;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Animator _animator;

        private bool _isResolved;
        private bool _isActive;
        private bool _isInteractionEnabled;
        private bool _shouldTickGimmick;
        private bool _isShieldBlockedDissolving;
        private bool _hasPendingEntryDelay;
        private float _shieldBlockedDissolveElapsed;
        private float _entryDelayElapsed;
        private float _entryDelayDuration;
        private Action<PlatformController> _onReleaseAction;
        private IPlatformGimmickBehaviour _gimmickBehaviour;
        private PlatformGimmickType _gimmickType;
        private PlatformVisual _visual;
        private PlatformMotion _motion;
        private PlatformGimmickTimer _gimmickTimer;
        private Vector3 _shieldBlockedDissolveStartPosition;
        private Vector3 _shieldBlockedDissolveTargetPosition;
        private IEventBus _eventBus;
        private PlayerRegistry _playerRegistry;
        private PlatformConfigData _platformConfigData;
        private PlayerConfigData _playerConfigData;

        public void Bind(
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            PlatformConfigData platformConfigData,
            PlayerConfigData playerConfigData,
            UnityEngine.Camera gameCamera)
        {
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _platformConfigData = platformConfigData;
            _playerConfigData = playerConfigData;
            _visual.BindCamera(gameCamera);
            _visual.CacheBaseSize(_platformConfigData);
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
            _gimmickType = gimmickSetting == null ? PlatformGimmickType.Normal : gimmickSetting.Type;
            _gimmickBehaviour = gimmickBehaviour;
            _visual.CacheBaseSize(_platformConfigData);
            ResetForSpawn();
            PlaceRigidbody(position);
            _gimmickBehaviour?.Reset(this);
            _gimmickBehaviour?.Apply(this, gimmickSetting);
            gameObject.SetActive(true);
        }

        public Vector3 GetLandingPosition(Player player)
        {
            var platformPosition = transform.position;
            return new Vector3(platformPosition.x, GetLandingSurfaceY() + player.GroundContactOffset, platformPosition.z);
        }

        public float GetStackedNextCenterY()
        {
            return transform.position.y + ResolveStackHeight() + _platformConfigData.PlatformStackVerticalOffset;
        }

        public bool TryResolveLanding(Player player)
        {
            if (!_isActive || !_isInteractionEnabled || _isResolved || player == null)
            {
                return false;
            }

            var platformPosition = transform.position;
            var halfWidth = _visual.ResolveLandingHalfWidth();
            if (!PlatformLandingResolver.IsPlayerWithinContact(player, platformPosition, halfWidth, _playerConfigData))
            {
                return false;
            }

            var landingY = GetLandingSurfaceY();
            if (PlatformLandingResolver.CanResolveLanding(player, landingY))
            {
                if (player.TryBlockWithShield())
                {
                    ResolveShieldBlock(player);
                    return true;
                }

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
            if (!PlatformLandingResolver.CanResolveStackedLanding(player, landingY, _playerConfigData))
            {
                return false;
            }

            ResolveStackedLanding(player, landingY);
            return true;
        }

        public void Release()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _isShieldBlockedDissolving = false;
            _hasPendingEntryDelay = false;
            _gimmickBehaviour?.Reset(this);
            ResetGimmickRuntime();
            gameObject.SetActive(false);
            _onReleaseAction?.Invoke(this);
        }

        private void Awake()
        {
            _visual.BindComponents(_spriteRenderer, _landingCollider, _animator);
        }

        private void FixedUpdate()
        {
            if (!_isActive)
            {
                return;
            }

            if (_isShieldBlockedDissolving)
            {
                TickShieldBlockedDissolve(Time.fixedDeltaTime);
                return;
            }

            if (TickPendingEntryDelay(Time.fixedDeltaTime))
            {
                return;
            }

            if (_isResolved)
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

        internal void ApplyWidthScale(float widthScale) =>
            _visual.ApplyScale(Mathf.Max(GameConst.Platform.MinimumWidthScale, widthScale), 1f);

        internal void ApplyMoveSpeedScale(float moveSpeedScale) => _motion.ApplySpeedScale(moveSpeedScale);

        internal void PrepareGimmickTimer(float duration) => _gimmickTimer.Prepare(duration);

        internal void StartPreparedGimmickTimer()
        {
            _gimmickTimer.Start();
            EnableGimmickTick();
        }

        internal void EnableGimmickTick() =>
            _shouldTickGimmick = _gimmickBehaviour != null && _gimmickBehaviour.RequiresTick;

        internal float AdvanceGimmickTimer(float deltaTime) => _gimmickTimer.Advance(deltaTime);

        internal bool IsGimmickTimerRunning => _gimmickTimer.IsRunning;
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
            _gimmickTimer.Stop();
        }

        internal void DelayMove(float delay)
        {
            if (delay <= 0f)
            {
                return;
            }

            _motion.Pause();
            _entryDelayElapsed = 0f;
            _entryDelayDuration = delay;
            _hasPendingEntryDelay = true;
        }

        internal void RevealPlatformVisual()
        {
            ResetGimmickRuntime();
            SetPlatformAlpha(1f);
        }

        internal void ApplySprite(Sprite sprite) => _visual.ApplySprite(sprite);
        internal void SetPlatformAlpha(float alpha) => _visual.SetAlpha(alpha);
        internal bool IsFullyInGameCameraView() => _visual.IsFullyInGameCameraView();
        internal bool IsMostlyInGameCameraView(float visibleRatio) => _visual.IsMostlyInGameCameraView(visibleRatio);

        private void ResetForSpawn()
        {
            _isResolved = false;
            _isActive = true;
            _isInteractionEnabled = true;
            _isShieldBlockedDissolving = false;
            _hasPendingEntryDelay = false;
            _shieldBlockedDissolveElapsed = 0f;
            _entryDelayElapsed = 0f;
            _entryDelayDuration = 0f;
            ResetGimmickRuntime();
            SetPlatformAlpha(1f);
            _visual.ApplyScale(1f, 1f);
            _visual.SetLandingColliderEnabled(true);
            _visual.SetLandingColliderTrigger(true);
        }

        private void ResetGimmickRuntime()
        {
            _gimmickTimer.Reset();
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
                    _playerConfigData,
                    _platformConfigData))
            {
                ResolveSideHit(player);
            }
        }

        private float GetLandingSurfaceY() => _visual.GetLandingSurfaceY(_platformConfigData.PlatformLandingHeight);
        private float ResolveStackHeight() => _visual.ResolveStackHeight(_platformConfigData.PlatformHeight);

        private void ResolveLanding(Player player, float landingY)
        {
            _isResolved = true;
            _motion.Stop();
            _gimmickBehaviour?.OnLanding(this, player);
            SetLandingColliderTrigger(false);
            PlayJumpAnimation();
            player.LandOnPlatform(this, BuildLandingPosition(player, landingY, false));
        }

        private void ResolveStackedLanding(Player player, float landingY)
        {
            _gimmickBehaviour?.OnLanding(this, player);
            PlayJumpAnimation();
            player.LandOnStackedPlatform(this, BuildLandingPosition(player, landingY, false));
        }

        private Vector3 BuildLandingPosition(Player player, float landingY, bool snapToPlatformX)
        {
            var landingPosition = player.Position;
            if (snapToPlatformX)
            {
                landingPosition.x = transform.position.x;
            }

            landingPosition.y = landingY + player.GroundContactOffset;
            return landingPosition;
        }

        private void PlayJumpAnimation() => _visual.PlayJumpAnimation();

        private void ResolveSideHit(Player player)
        {
            if (player.TryBlockWithShield())
            {
                ResolveShieldBlock(player);
                return;
            }

            _isResolved = true;
            _motion.Stop();
            StopGimmickTick();
            var knockbackDirection = PlatformLandingResolver.ResolveKnockbackDirection(
                player,
                transform.position,
                _motion.MoveDirectionX);
            _eventBus.Publish(new PlayerMissedLandingEvent(knockbackDirection));
        }

        private void ResolveShieldBlock(Player player)
        {
            _isResolved = true;
            _motion.Stop();
            StopGimmickTick();
            SetInteractionEnabled(false);
            StartShieldBlockedDissolve(player);
        }

        private void StartShieldBlockedDissolve(Player player)
        {
            _isShieldBlockedDissolving = true;
            _shieldBlockedDissolveElapsed = 0f;
            _shieldBlockedDissolveStartPosition = transform.position;
            var retreatDirectionX = ResolveShieldBlockedRetreatDirectionX(player);
            _shieldBlockedDissolveTargetPosition = _shieldBlockedDissolveStartPosition +
                                                   new Vector3(
                                                       retreatDirectionX * GameConst.Platform.ShieldBlockedRetreatDistance,
                                                       0f,
                                                       0f);
        }

        private void TickShieldBlockedDissolve(float deltaTime)
        {
            _shieldBlockedDissolveElapsed += deltaTime;
            var normalizedTime = Mathf.Clamp01(
                _shieldBlockedDissolveElapsed / Mathf.Max(0.01f, GameConst.Platform.ShieldBlockedFadeDuration));
            var easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            var nextPosition = Vector3.Lerp(
                _shieldBlockedDissolveStartPosition,
                _shieldBlockedDissolveTargetPosition,
                easedTime);
            _rigidbody.MovePosition(nextPosition);
            transform.position = nextPosition;
            SetPlatformAlpha(1f - easedTime);

            if (normalizedTime >= 1f)
            {
                _eventBus.Publish(new PlatformShieldBlockedEvent(this));
                Release();
            }
        }

        private float ResolveShieldBlockedRetreatDirectionX(Player player)
        {
            var directionX = -_motion.MoveDirectionX;
            if (Mathf.Approximately(directionX, 0f))
            {
                directionX = Mathf.Sign(transform.position.x - player.Position.x);
            }

            if (Mathf.Approximately(directionX, 0f))
            {
                directionX = 1f;
            }

            return directionX;
        }

        private bool TickPendingEntryDelay(float deltaTime)
        {
            if (!_hasPendingEntryDelay)
            {
                return false;
            }

            _entryDelayElapsed += deltaTime;
            if (_entryDelayElapsed < _entryDelayDuration)
            {
                return true;
            }

            _hasPendingEntryDelay = false;
            _entryDelayElapsed = 0f;
            _entryDelayDuration = 0f;
            _motion.Resume();
            return false;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryResolveLanding(ResolvePlayerFromCollider(other));
        private void OnTriggerStay2D(Collider2D other) => TryResolveLanding(ResolvePlayerFromCollider(other));
        private void OnCollisionEnter2D(Collision2D collision) => TryResolveStackedLanding(ResolvePlayerFromCollider(collision.collider));
        private void OnCollisionStay2D(Collision2D collision) => TryResolveStackedLanding(ResolvePlayerFromCollider(collision.collider));

        private Player ResolvePlayerFromCollider(Collider2D other) =>
            PlatformLandingResolver.ResolvePlayerFromCollider(other, _playerRegistry.Player);

        private void SetLandingColliderTrigger(bool isTrigger) => _visual.SetLandingColliderTrigger(isTrigger);
    }
}
