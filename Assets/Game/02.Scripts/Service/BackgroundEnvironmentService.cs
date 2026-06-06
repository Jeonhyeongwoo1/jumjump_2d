using System;
using JumJump.Camera;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class BackgroundEnvironmentService : IInitializable, ITickable, IDisposable
    {
        private struct BackgroundItem
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float ParallaxFactor;
        }

        private readonly IEventBus _eventBus;
        private readonly GameConfigData _configData;
        private readonly BackgroundEnvironmentFactory _factory;
        private readonly VerticalFollowCamera _followCamera;

        private BackgroundItem[] _items;
        private Transform _root;
        private SpriteRenderer _gradientRenderer;
        private Texture2D _gradientTexture;
        private UnityEngine.Camera _camera;
        private float _lastGradientT = -1f;
        private float _previousCameraY;
        private bool _isInitialized;

        public BackgroundEnvironmentService(
            IEventBus eventBus,
            GameConfigData configData,
            BackgroundEnvironmentFactory factory,
            VerticalFollowCamera followCamera)
        {
            _eventBus = eventBus;
            _configData = configData;
            _factory = factory;
            _followCamera = followCamera;
        }

        public void Initialize()
        {
            _camera = _followCamera == null ? null : _followCamera.GetComponent<UnityEngine.Camera>();
            if (_camera == null)
            {
                Debug.LogError($"[{nameof(BackgroundEnvironmentService)}] Missing scene camera.");
                return;
            }

            _root = _factory.CreateRoot("BackgroundEnvironment");
            CreateGradientLayer();
            CreateBackgroundItems();
            _previousCameraY = ResolveCameraY();
            RebuildAllItems();
            _eventBus.Subscribe<GameResetEvent>(OnGameReset);
            _isInitialized = true;
        }

        public void Tick()
        {
            if (!_isInitialized)
            {
                return;
            }

            var cameraY = ResolveCameraY();
            var cameraDeltaY = cameraY - _previousCameraY;
            _previousCameraY = cameraY;
            UpdateGradient();
            UpdateGradientTransform();
            UpdateBackgroundItems(cameraY, cameraDeltaY);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResetEvent>(OnGameReset);
        }

        private void OnGameReset(in GameResetEvent ev)
        {
            _previousCameraY = ResolveCameraY();
            RebuildAllItems();
        }

        private void CreateGradientLayer()
        {
            _gradientRenderer = _factory.CreateSpriteRenderer(
                "HeightGradient",
                _root,
                _configData.BackgroundGradientSortingOrder);
            _gradientRenderer.sprite = _factory.CreateGradientSprite();
            _gradientTexture = _gradientRenderer.sprite.texture;
            UpdateGradient();
            UpdateGradientTransform();
        }

        private void CreateBackgroundItems()
        {
            var itemCount = Mathf.Max(0, _configData.BackgroundObjectPoolCount);
            _items = new BackgroundItem[itemCount];

            for (var i = 0; i < itemCount; i++)
            {
                var renderer = _factory.CreateSpriteRenderer(
                    $"BackgroundObject_{i:00}",
                    _root,
                    _configData.BackgroundObjectSortingOrder);
                _items[i] = new BackgroundItem
                {
                    Transform = renderer.transform,
                    Renderer = renderer,
                    ParallaxFactor = UnityEngine.Random.Range(
                        _configData.BackgroundObjectMinParallax,
                        _configData.BackgroundObjectMaxParallax)
                };
            }
        }

        private void RebuildAllItems()
        {
            if (_items == null)
            {
                return;
            }

            var cameraY = ResolveCameraY();
            var startY = cameraY - _configData.BackgroundObjectRecycleBelowY;
            var yRange = _configData.BackgroundObjectRecycleBelowY + _configData.BackgroundObjectSpawnAheadY;

            for (var i = 0; i < _items.Length; i++)
            {
                var itemY = startY + UnityEngine.Random.Range(0f, Mathf.Max(0.01f, yRange));
                RepositionItem(i, itemY);
            }
        }

        private void UpdateGradient()
        {
            var gradientT = ResolveHeightT(ResolveCameraY());
            if (Mathf.Abs(gradientT - _lastGradientT) < _configData.BackgroundGradientUpdateThreshold)
            {
                return;
            }

            _lastGradientT = gradientT;
            var bottomColor = Color.Lerp(
                _configData.BackgroundLowBottomColor,
                _configData.BackgroundHighBottomColor,
                gradientT);
            var topColor = Color.Lerp(
                _configData.BackgroundLowTopColor,
                _configData.BackgroundHighTopColor,
                gradientT);

            for (var y = 0; y < _gradientTexture.height; y++)
            {
                var rowT = y / Mathf.Max(1f, _gradientTexture.height - 1f);
                var rowColor = Color.Lerp(bottomColor, topColor, rowT);
                for (var x = 0; x < _gradientTexture.width; x++)
                {
                    _gradientTexture.SetPixel(x, y, rowColor);
                }
            }

            _gradientTexture.Apply(false, false);
        }

        private void UpdateGradientTransform()
        {
            var cameraTransform = _camera.transform;
            var position = cameraTransform.position;
            position.z = _configData.BackgroundGradientZ;
            _gradientRenderer.transform.position = position;

            var verticalSize = _camera.orthographicSize * 2f * _configData.BackgroundGradientScreenPadding;
            var horizontalSize = verticalSize * _camera.aspect;
            _gradientRenderer.transform.localScale = new Vector3(horizontalSize, verticalSize, 1f);
        }

        private void UpdateBackgroundItems(float cameraY, float cameraDeltaY)
        {
            var recycleY = cameraY - _configData.BackgroundObjectRecycleBelowY;

            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item.Transform.position.y < recycleY)
                {
                    RepositionItem(i, cameraY + _configData.BackgroundObjectSpawnAheadY);
                    continue;
                }

                ApplyParallax(i, cameraDeltaY);
            }
        }

        private void RepositionItem(int index, float y)
        {
            var item = _items[index];
            var sprite = ResolveThemeSprite(y);
            if (sprite == null)
            {
                item.Renderer.gameObject.SetActive(false);
                return;
            }

            item.Renderer.gameObject.SetActive(true);
            item.Renderer.sprite = sprite;
            item.Renderer.color = ResolveObjectColor(y);

            var scale = UnityEngine.Random.Range(
                _configData.BackgroundObjectMinScale,
                _configData.BackgroundObjectMaxScale);
            item.Transform.localScale = new Vector3(scale, scale, 1f);
            item.Transform.position = new Vector3(
                UnityEngine.Random.Range(-_configData.BackgroundObjectXRange, _configData.BackgroundObjectXRange),
                y,
                _configData.BackgroundObjectZ);
        }

        private void ApplyParallax(int index, float cameraDeltaY)
        {
            if (Mathf.Approximately(cameraDeltaY, 0f))
            {
                return;
            }

            var item = _items[index];
            var position = item.Transform.position;
            position.y += cameraDeltaY * Mathf.Clamp01(1f - item.ParallaxFactor);
            item.Transform.position = position;
        }

        private Sprite ResolveThemeSprite(float height)
        {
            if (height >= _configData.BackgroundNightThemeStartHeight)
            {
                return PickSprite(_configData.BackgroundNightSprites);
            }

            if (height >= _configData.BackgroundSkyThemeStartHeight)
            {
                return PickSprite(_configData.BackgroundSkySprites);
            }

            return PickSprite(_configData.BackgroundGroundSprites);
        }

        private Sprite PickSprite(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return null;
            }

            return sprites[UnityEngine.Random.Range(0, sprites.Length)];
        }

        private Color ResolveObjectColor(float height)
        {
            return Color.Lerp(
                Color.white,
                _configData.BackgroundHighObjectTint,
                ResolveHeightT(height));
        }

        private float ResolveHeightT(float height)
        {
            return Mathf.InverseLerp(
                _configData.BackgroundGradientMinHeight,
                _configData.BackgroundGradientMaxHeight,
                height);
        }

        private float ResolveCameraY()
        {
            return _camera == null ? 0f : _camera.transform.position.y;
        }
    }
}
