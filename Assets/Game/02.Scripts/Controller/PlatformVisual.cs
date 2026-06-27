using JumJump.Data;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Controller
{
    internal struct PlatformVisual
    {
        private const string JumpAnimationStateName = "JumpAnimation";
        private const string RocketAnimationStateName = "RoketAnimation";
        private const string ShieldAnimationStateName = "ShieldAnimation";
        private const string JumpAnimationStatePath = "Base Layer.JumpAnimation";
        private const string RocketAnimationStatePath = "Base Layer.RoketAnimation";
        private const string ShieldAnimationStatePath = "Base Layer.ShieldAnimation";
        private const float ComboPulseDuration = 0.28f;
        private const float ComboPulseInitialProgress = 0.12f;
        private const float ComboPulseBaseScale = 0.045f;
        private const float ComboPulseScalePerCombo = 0.006f;

        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _jumpSpriteRenderer;
        private BoxCollider2D _landingCollider;
        private Animator _animator;
        private UnityEngine.Camera _gameCamera;
        private int _jumpAnimationShortNameHash;
        private int _rocketAnimationShortNameHash;
        private int _shieldAnimationShortNameHash;
        private int _jumpAnimationPathHash;
        private int _rocketAnimationPathHash;
        private int _shieldAnimationPathHash;
        private int _activeJumpAnimationShortNameHash;
        private int _activeJumpAnimationPathHash;
        private Sprite _baseSprite;
        private Vector3 _baseLocalScale;
        private Vector3 _baseSpriteLocalScale;
        private Vector3 _baseJumpSpriteLocalScale;
        private Color _baseSpriteColor;
        private Color _baseJumpSpriteColor;
        private Vector2 _baseSpriteSize;
        private Vector2 _baseJumpSpriteSize;
        private Vector2 _baseColliderSize;
        private bool _hasCachedBaseSize;
        private bool _isJumpAnimationPlaying;
        private float _widthScale;
        private float _heightScale;
        private float _alpha;
        private float _comboPulseElapsed;
        private float _comboPulseScale;
        private bool _isComboPulsePlaying;

        public void BindComponents(
            SpriteRenderer spriteRenderer,
            SpriteRenderer jumpSpriteRenderer,
            BoxCollider2D landingCollider,
            Animator animator)
        {
            _spriteRenderer = spriteRenderer;
            _jumpSpriteRenderer = jumpSpriteRenderer;
            _landingCollider = landingCollider;
            _animator = animator;
            _jumpAnimationShortNameHash = Animator.StringToHash(JumpAnimationStateName);
            _rocketAnimationShortNameHash = Animator.StringToHash(RocketAnimationStateName);
            _shieldAnimationShortNameHash = Animator.StringToHash(ShieldAnimationStateName);
            _jumpAnimationPathHash = Animator.StringToHash(JumpAnimationStatePath);
            _rocketAnimationPathHash = Animator.StringToHash(RocketAnimationStatePath);
            _shieldAnimationPathHash = Animator.StringToHash(ShieldAnimationStatePath);
            _hasCachedBaseSize = false;
            _widthScale = 1f;
            _heightScale = 1f;
            _alpha = 1f;
            StopComboPulse();
            HideJumpAnimation();
        }

        public void BindCamera(UnityEngine.Camera gameCamera)
        {
            _gameCamera = gameCamera;
        }

        public void CacheBaseSize(PlatformConfigData configData)
        {
            if (_hasCachedBaseSize)
            {
                return;
            }

            _baseLocalScale = _landingCollider.transform.localScale;
            _baseSprite = _spriteRenderer.sprite;
            _baseSpriteLocalScale = _spriteRenderer.transform.localScale;
            _baseJumpSpriteLocalScale = _jumpSpriteRenderer.transform.localScale;
            _baseSpriteColor = _spriteRenderer.color;
            _baseJumpSpriteColor = _jumpSpriteRenderer.color;
            _baseSpriteSize = _spriteRenderer.size;
            _baseJumpSpriteSize = _jumpSpriteRenderer.size;
            _baseColliderSize = ResolveBaseColliderSize(configData);
            _hasCachedBaseSize = true;
        }

        public void ApplyScale(float widthScale, float heightScale)
        {
            _widthScale = widthScale;
            _heightScale = heightScale;
            ApplyResolvedScale();
        }

        public void ApplySprite(Sprite sprite)
        {
            _spriteRenderer.sprite = sprite == null ? _baseSprite : sprite;
        }

        public void SetAlpha(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            ApplyResolvedColors();
        }

        public void PlayComboPulse(int comboCount)
        {
            var clampedCombo = Mathf.Min(Mathf.Max(0, comboCount), 8);
            _comboPulseElapsed = ComboPulseDuration * ComboPulseInitialProgress;
            _comboPulseScale = ComboPulseBaseScale + clampedCombo * ComboPulseScalePerCombo;
            _isComboPulsePlaying = true;
            ApplyResolvedScale();
            ApplyResolvedColors();
        }

        public void StopComboPulse()
        {
            _comboPulseElapsed = 0f;
            _comboPulseScale = 0f;
            _isComboPulsePlaying = false;

            if (_hasCachedBaseSize)
            {
                ApplyResolvedScale();
                ApplyResolvedColors();
            }
        }

        public void TickComboPulse(float deltaTime)
        {
            if (!_isComboPulsePlaying)
            {
                return;
            }

            _comboPulseElapsed += deltaTime;
            if (_comboPulseElapsed >= ComboPulseDuration)
            {
                StopComboPulse();
            }

            ApplyResolvedScale();
            ApplyResolvedColors();
        }

        public void PlayJumpAnimation(PlatformGimmickType gimmickType)
        {
            _activeJumpAnimationShortNameHash = ResolveJumpAnimationShortNameHash(gimmickType);
            _activeJumpAnimationPathHash = ResolveJumpAnimationPathHash(gimmickType);
            _isJumpAnimationPlaying = true;
            _spriteRenderer.enabled = false;
            _jumpSpriteRenderer.enabled = true;
            _animator.Play(_activeJumpAnimationPathHash, 0, 0f);
            _animator.Update(0f);
        }

        public void TickJumpAnimation()
        {
            if (!_isJumpAnimationPlaying)
            {
                return;
            }

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if ((stateInfo.fullPathHash != _activeJumpAnimationPathHash &&
                 stateInfo.shortNameHash != _activeJumpAnimationShortNameHash) ||
                stateInfo.normalizedTime < 1f ||
                _animator.IsInTransition(0))
            {
                return;
            }

            HideJumpAnimation();
        }

        public void HideJumpAnimation()
        {
            _isJumpAnimationPlaying = false;
            _activeJumpAnimationShortNameHash = 0;
            _activeJumpAnimationPathHash = 0;
            _jumpSpriteRenderer.enabled = false;
            _spriteRenderer.enabled = true;
        }

        public void SetLandingColliderEnabled(bool isEnabled)
        {
            _landingCollider.enabled = isEnabled;
        }

        public void SetLandingColliderTrigger(bool isTrigger)
        {
            _landingCollider.isTrigger = isTrigger;
        }

        public float GetBottomY() => _spriteRenderer.bounds.min.y;

        public float GetTopY() => _spriteRenderer.bounds.max.y;

        public float ResolveLandingHalfWidth()
        {
            return Mathf.Max(GameConst.Platform.MinimumColliderDimension, _landingCollider.bounds.size.x * 0.5f);
        }

        public float GetLandingSurfaceY(float fallbackLandingHeight)
        {
            if (_landingCollider.enabled)
            {
                return _landingCollider.bounds.max.y;
            }

            return _landingCollider.transform.position.y + fallbackLandingHeight * 0.5f;
        }

        public float ResolveStackHeight(float fallbackPlatformHeight)
        {
            if (_landingCollider.enabled)
            {
                return Mathf.Max(0f, _landingCollider.bounds.size.y);
            }

            return Mathf.Max(0f, fallbackPlatformHeight);
        }

        public bool IsFullyInGameCameraView()
        {
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

        public bool IsMostlyInGameCameraView(float visibleRatio)
        {
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

        private void ApplyResolvedScale()
        {
            var rootTransform = _landingCollider.transform;
            var spriteOnRoot = _spriteRenderer.transform == rootTransform;
            var visualScale = ResolveComboPulseScale();
            rootTransform.localScale = spriteOnRoot
                ? new Vector3(_baseLocalScale.x * _widthScale, _baseLocalScale.y * _heightScale, _baseLocalScale.z)
                : _baseLocalScale;

            if (_spriteRenderer.drawMode == SpriteDrawMode.Sliced ||
                _spriteRenderer.drawMode == SpriteDrawMode.Tiled)
            {
                _spriteRenderer.size = spriteOnRoot
                    ? _baseSpriteSize
                    : new Vector2(
                        _baseSpriteSize.x * _widthScale,
                        _baseSpriteSize.y * _heightScale);
                if (!spriteOnRoot)
                {
                    _spriteRenderer.transform.localScale = _baseSpriteLocalScale * visualScale;
                }
            }
            else if (!spriteOnRoot)
            {
                _spriteRenderer.transform.localScale = new Vector3(
                    _baseSpriteLocalScale.x * _widthScale * visualScale,
                    _baseSpriteLocalScale.y * _heightScale * visualScale,
                    _baseSpriteLocalScale.z);
            }

            ApplyJumpSpriteScale(_widthScale, _heightScale, visualScale);

            _landingCollider.size = new Vector2(
                _baseColliderSize.x * (spriteOnRoot ? 1f : _widthScale),
                _baseColliderSize.y * (spriteOnRoot ? 1f : _heightScale));
            _landingCollider.isTrigger = true;
        }

        private void ApplyResolvedColors()
        {
            var color = _baseSpriteColor;
            color.a = _baseSpriteColor.a * _alpha;
            _spriteRenderer.color = color;

            var jumpColor = _baseJumpSpriteColor;
            jumpColor.a = _baseJumpSpriteColor.a * _alpha;
            _jumpSpriteRenderer.color = jumpColor;
        }

        private float ResolveComboPulseScale()
        {
            return 1f + ResolveComboPulseStrength() * _comboPulseScale;
        }

        private float ResolveComboPulseStrength()
        {
            if (!_isComboPulsePlaying || ComboPulseDuration <= 0f)
            {
                return 0f;
            }

            var normalized = Mathf.Clamp01(_comboPulseElapsed / ComboPulseDuration);
            return Mathf.Sin(normalized * Mathf.PI);
        }

        private Vector2 ResolveBaseColliderSize(PlatformConfigData configData)
        {
            var colliderSize = _landingCollider.size;
            if (colliderSize.x < GameConst.Platform.MinimumColliderDimension)
            {
                colliderSize.x = Mathf.Max(GameConst.Platform.MinimumColliderDimension, configData.PlatformWidth);
            }

            if (colliderSize.y < GameConst.Platform.MinimumColliderDimension)
            {
                colliderSize.y = Mathf.Max(GameConst.Platform.MinimumColliderDimension, configData.PlatformLandingHeight);
            }

            return colliderSize;
        }

        private void ApplyJumpSpriteScale(float widthScale, float heightScale, float visualScale)
        {
            if (_jumpSpriteRenderer.drawMode == SpriteDrawMode.Sliced ||
                _jumpSpriteRenderer.drawMode == SpriteDrawMode.Tiled)
            {
                _jumpSpriteRenderer.size = new Vector2(
                    _baseJumpSpriteSize.x * widthScale,
                    _baseJumpSpriteSize.y * heightScale);
                _jumpSpriteRenderer.transform.localScale = _baseJumpSpriteLocalScale * visualScale;
                return;
            }

            _jumpSpriteRenderer.transform.localScale = new Vector3(
                _baseJumpSpriteLocalScale.x * widthScale * visualScale,
                _baseJumpSpriteLocalScale.y * heightScale * visualScale,
                _baseJumpSpriteLocalScale.z);
        }

        private int ResolveJumpAnimationShortNameHash(PlatformGimmickType gimmickType)
        {
            switch (gimmickType)
            {
                case PlatformGimmickType.Rocket:
                    return _rocketAnimationShortNameHash;
                case PlatformGimmickType.Shield:
                    return _shieldAnimationShortNameHash;
                default:
                    return _jumpAnimationShortNameHash;
            }
        }

        private int ResolveJumpAnimationPathHash(PlatformGimmickType gimmickType)
        {
            switch (gimmickType)
            {
                case PlatformGimmickType.Rocket:
                    return _rocketAnimationPathHash;
                case PlatformGimmickType.Shield:
                    return _shieldAnimationPathHash;
                default:
                    return _jumpAnimationPathHash;
            }
        }
    }
}
