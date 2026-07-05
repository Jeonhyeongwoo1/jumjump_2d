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
        public float CenterX => transform.position.x;
        public float CenterY => transform.position.y;
        public float BottomY => _visual.GetBottomY();
        public float TopY => _visual.GetTopY();
        public PlatformGimmickType GimmickType => _gimmickType;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _jumpSpriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Animator _animator;

        private PlatformStateType _state;
        private bool _isInteractionEnabled;
        private bool _shouldTickGimmick;
        private Action<PlatformController> _onReleaseAction;
        private IPlatformGimmickBehaviour _gimmickBehaviour;
        private PlatformGimmickType _gimmickType;
        private PlatformVisual _visual;
        private PlatformMotion _motion;
        private PlatformGimmickTimer _gimmickTimer;
        private PlatformEntryDelay _entryDelay;
        private PlatformShieldBlockedDissolve _shieldBlockedDissolve;
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

        public Vector3 GetBoostFXPosition()
        {
            var platformPosition = transform.position;
            platformPosition.y = GetLandingSurfaceY();
            return platformPosition;
        }

        public float GetStackedNextCenterY()
        {
            return transform.position.y + ResolveStackHeight() + _platformConfigData.PlatformStackVerticalOffset;
        }

        public bool TryResolveLanding(Player player)
        {
            if (_state != PlatformStateType.Moving || !_isInteractionEnabled || player == null)
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
            if (_state != PlatformStateType.Resolved || !_isInteractionEnabled || player == null ||
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
            if (_state == PlatformStateType.Released)
            {
                return;
            }

            _state = PlatformStateType.Released;
            _entryDelay.Reset();
            _shieldBlockedDissolve.Reset();
            _gimmickBehaviour?.Reset(this);
            ResetGimmickRuntime();
            gameObject.SetActive(false);
            _onReleaseAction?.Invoke(this);
        }

        public void ArchiveForResultView()
        {
            if (_state == PlatformStateType.Released || _state == PlatformStateType.Archived)
            {
                return;
            }

            PrepareResultViewState();
            gameObject.SetActive(false);
        }

        public void ShowForResultView()
        {
            if (_state == PlatformStateType.Released)
            {
                return;
            }

            PrepareResultViewState();
            gameObject.SetActive(true);
        }

        private void Awake()
        {
            _visual.BindComponents(_spriteRenderer, _jumpSpriteRenderer, _landingCollider, _animator);
        }

        private void FixedUpdate()
        {
            if (_state == PlatformStateType.Released)
            {
                return;
            }

            if (_state == PlatformStateType.ShieldBlockedDissolving)
            {
                TickShieldBlockedDissolve(Time.fixedDeltaTime);
                return;
            }

            if (TickPendingEntryDelay(Time.fixedDeltaTime))
            {
                return;
            }

            if (_state != PlatformStateType.Moving)
            {
                return;
            }

            _motion.MoveTowardTarget(transform, _rigidbody, Time.fixedDeltaTime);
            EvaluateMissedPlayer();
        }

        private void Update()
        {
            _visual.TickJumpAnimation();
            _visual.TickComboPulse(Time.deltaTime);

            if (_state != PlatformStateType.Moving || !_shouldTickGimmick || _gimmickBehaviour == null)
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
            SetInteractionEnabled(false);
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

        internal void DelayMove(float delay, bool hideWhileWaiting = false)
        {
            if (!_entryDelay.Start(delay, hideWhileWaiting))
            {
                return;
            }

            _motion.Pause();
            if (hideWhileWaiting)
            {
                SetPlatformAlpha(0f);
            }
        }

        internal void RevealPlatformVisual()
        {
            ResetGimmickRuntime();
            SetPlatformAlpha(1f);
        }

        internal void ApplySprite(Sprite sprite) => _visual.ApplySprite(sprite);
        internal void SetPlatformAlpha(float alpha) => _visual.SetAlpha(alpha);
        internal void PlayComboPulse(int comboCount) => _visual.PlayComboPulse(comboCount);
        internal bool IsFullyInGameCameraView() => _visual.IsFullyInGameCameraView();
        internal bool IsMostlyInGameCameraView(float visibleRatio) => _visual.IsMostlyInGameCameraView(visibleRatio);

        private void ResetForSpawn()
        {
            _state = PlatformStateType.Moving;
            _isInteractionEnabled = true;
            _entryDelay.Reset();
            _shieldBlockedDissolve.Reset();
            ResetGimmickRuntime();
            _visual.StopComboPulse();
            _visual.HideJumpAnimation();
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

        private void PrepareResultViewState()
        {
            _state = PlatformStateType.Archived;
            _motion.Stop();
            _entryDelay.Reset();
            _shieldBlockedDissolve.Reset();
            _gimmickBehaviour?.Reset(this);
            ResetGimmickRuntime();
            _visual.StopComboPulse();
            _visual.HideJumpAnimation();
            SetInteractionEnabled(false);
            SetPlatformAlpha(1f);
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
            _state = PlatformStateType.Resolved;
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

        private void PlayJumpAnimation() => _visual.PlayJumpAnimation(_gimmickType);

        private void ResolveSideHit(Player player)
        {
            if (player.TryBlockWithShield())
            {
                ResolveShieldBlock(player);
                return;
            }

            _state = PlatformStateType.Resolved;
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
            _state = PlatformStateType.ShieldBlockedDissolving;
            _motion.Stop();
            StopGimmickTick();
            SetInteractionEnabled(false);
            StartShieldBlockedDissolve(player);
        }

        private void StartShieldBlockedDissolve(Player player)
        {
            var retreatDirectionX = ResolveShieldBlockedRetreatDirectionX(player);
            _shieldBlockedDissolve.Start(
                transform.position,
                retreatDirectionX,
                GameConst.Platform.ShieldBlockedRetreatDistance);
        }

        private void TickShieldBlockedDissolve(float deltaTime)
        {
            var isComplete = _shieldBlockedDissolve.Tick(
                deltaTime,
                GameConst.Platform.ShieldBlockedFadeDuration,
                out var nextPosition,
                out var alpha);
            _rigidbody.MovePosition(nextPosition);
            transform.position = nextPosition;
            SetPlatformAlpha(alpha);

            if (isComplete)
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
            if (!_entryDelay.IsWaiting)
            {
                return false;
            }

            var wasHidden = _entryDelay.HideWhileWaiting;
            if (_entryDelay.Tick(deltaTime))
            {
                return true;
            }

            _motion.Resume();
            if (wasHidden)
            {
                SetPlatformAlpha(1f);
            }

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
