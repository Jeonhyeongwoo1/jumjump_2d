using JumJump.Data;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Controller
{
    internal struct PlatformVisual
    {
        private const string JumpAnimationStateName = "JumpAnimation";

        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _jumpSpriteRenderer;
        private BoxCollider2D _landingCollider;
        private Animator _animator;
        private UnityEngine.Camera _gameCamera;
        private float _jumpAnimationRemaining;
        private int _jumpAnimationStateHash;
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
            _jumpAnimationStateHash = Animator.StringToHash(JumpAnimationStateName);
            _hasCachedBaseSize = false;
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
            var rootTransform = _landingCollider.transform;
            var spriteOnRoot = _spriteRenderer.transform == rootTransform;
            rootTransform.localScale = spriteOnRoot
                ? new Vector3(_baseLocalScale.x * widthScale, _baseLocalScale.y * heightScale, _baseLocalScale.z)
                : _baseLocalScale;

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

            ApplyJumpSpriteScale(widthScale, heightScale);

            var colliderWidthScale = spriteOnRoot ? 1f : widthScale;
            var colliderHeightScale = spriteOnRoot ? 1f : heightScale;
            _landingCollider.size = new Vector2(
                _baseColliderSize.x * colliderWidthScale,
                _baseColliderSize.y * colliderHeightScale);
            _landingCollider.isTrigger = true;
        }

        public void ApplySprite(Sprite sprite)
        {
            _spriteRenderer.sprite = sprite == null ? _baseSprite : sprite;
        }

        public void SetAlpha(float alpha)
        {
            var color = _baseSpriteColor;
            color.a *= Mathf.Clamp01(alpha);
            _spriteRenderer.color = color;

            var jumpColor = _baseJumpSpriteColor;
            jumpColor.a *= Mathf.Clamp01(alpha);
            _jumpSpriteRenderer.color = jumpColor;
        }

        public void PlayJumpAnimation()
        {
            _jumpAnimationRemaining = GameConst.Platform.JumpAnimationDuration;
            _jumpSpriteRenderer.enabled = true;
            _animator.Play(_jumpAnimationStateHash, 0, 0f);
        }

        public void TickJumpAnimation(float deltaTime)
        {
            if (_jumpAnimationRemaining <= 0f)
            {
                return;
            }

            _jumpAnimationRemaining -= deltaTime;
            if (_jumpAnimationRemaining <= 0f)
            {
                HideJumpAnimation();
            }
        }

        public void HideJumpAnimation()
        {
            _jumpAnimationRemaining = 0f;
            _jumpSpriteRenderer.enabled = false;
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

        private void ApplyJumpSpriteScale(float widthScale, float heightScale)
        {
            if (_jumpSpriteRenderer.drawMode == SpriteDrawMode.Sliced ||
                _jumpSpriteRenderer.drawMode == SpriteDrawMode.Tiled)
            {
                _jumpSpriteRenderer.size = new Vector2(
                    _baseJumpSpriteSize.x * widthScale,
                    _baseJumpSpriteSize.y * heightScale);
                _jumpSpriteRenderer.transform.localScale = _baseJumpSpriteLocalScale;
                return;
            }

            _jumpSpriteRenderer.transform.localScale = new Vector3(
                _baseJumpSpriteLocalScale.x * widthScale,
                _baseJumpSpriteLocalScale.y * heightScale,
                _baseJumpSpriteLocalScale.z);
        }
    }
}
