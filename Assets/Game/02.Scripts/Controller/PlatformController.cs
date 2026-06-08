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

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Animator _animator;

        private bool _isResolved;
        private bool _isActive;
        private bool _isInteractionEnabled;
        private bool _shouldTickGimmick;
        private Action<PlatformController> _onReleaseAction;
        private IPlatformGimmickBehaviour _gimmickBehaviour;
        private PlatformVisual _visual;
        private PlatformMotion _motion;
        private PlatformGimmickTimer _gimmickTimer;
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

        public Vector3 GetLandingPosition(Player player)
        {
            var platformPosition = transform.position;
            return new Vector3(platformPosition.x, GetLandingSurfaceY() + player.GroundContactOffset, platformPosition.z);
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

        internal void RevealPlatformVisual()
        {
            ResetGimmickRuntime();
            SetPlatformAlpha(1f);
        }

        internal void SetPlatformAlpha(float alpha) => _visual.SetAlpha(alpha);
        internal bool IsFullyInGameCameraView() => _visual.IsFullyInGameCameraView();
        internal bool IsMostlyInGameCameraView(float visibleRatio) => _visual.IsMostlyInGameCameraView(visibleRatio);

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
                    _configData))
            {
                ResolveSideHit(player);
            }
        }

        private float GetLandingSurfaceY() => _visual.GetLandingSurfaceY(_configData.PlatformLandingHeight);
        private float ResolveStackHeight() => _visual.ResolveStackHeight(_configData.PlatformHeight);

        private void ResolveLanding(Player player, float landingY)
        {
            _isResolved = true;
            _motion.Stop();
            _gimmickBehaviour?.OnLanding(this);
            SetLandingColliderTrigger(false);
            PlayJumpAnimation();
            player.LandOnPlatform(this, BuildLandingPosition(player, landingY));
        }

        private void ResolveStackedLanding(Player player, float landingY)
        {
            _gimmickBehaviour?.OnLanding(this);
            PlayJumpAnimation();
            player.LandOnStackedPlatform(this, BuildLandingPosition(player, landingY));
        }

        private Vector3 BuildLandingPosition(Player player, float landingY)
        {
            var landingPosition = player.Position;
            landingPosition.y = landingY + player.GroundContactOffset;
            return landingPosition;
        }

        private void PlayJumpAnimation() => _visual.PlayJumpAnimation();

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

        private void OnTriggerEnter2D(Collider2D other) => TryResolveLanding(ResolvePlayerFromCollider(other));
        private void OnTriggerStay2D(Collider2D other) => TryResolveLanding(ResolvePlayerFromCollider(other));
        private void OnCollisionEnter2D(Collision2D collision) => TryResolveStackedLanding(ResolvePlayerFromCollider(collision.collider));
        private void OnCollisionStay2D(Collision2D collision) => TryResolveStackedLanding(ResolvePlayerFromCollider(collision.collider));

        private Player ResolvePlayerFromCollider(Collider2D other) =>
            PlatformLandingResolver.ResolvePlayerFromCollider(other, _playerRegistry.Player);

        private void SetLandingColliderTrigger(bool isTrigger) => _visual.SetLandingColliderTrigger(isTrigger);
    }
}
