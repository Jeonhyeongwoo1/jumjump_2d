using JumJump.Controller;
using JumJump.Data;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Service;
using UnityEngine;

namespace JumJump.Factory
{
    public sealed class PlatformFactory
    {
        private readonly Transform _poolRoot;
        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;
        private readonly IEventBus _eventBus;
        private readonly PlayerRegistry _playerRegistry;
        private readonly GameConfigData _configData;
        private readonly UnityEngine.Camera _gameCamera;

        private PlatformController _platformPrefab;
        private bool _isReady;

        public PlatformFactory(
            Transform poolRoot,
            PoolService poolService,
            ResourceService resourceService,
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            GameConfigData configData,
            UnityEngine.Camera gameCamera)
        {
            _poolRoot = poolRoot;
            _poolService = poolService;
            _resourceService = resourceService;
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _configData = configData;
            _gameCamera = gameCamera;
        }

        public void Warmup()
        {
            if (_isReady)
            {
                return;
            }

            var prefab = _resourceService.GetPrefab(_configData.PlatformAddressableKey);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Failed to load platform prefab: {_configData.PlatformAddressableKey}");
                return;
            }

            _platformPrefab = prefab.GetComponent<PlatformController>();
            _poolService.Register(_configData.PlatformPoolKey, Create, OnGet, OnRelease, _configData.PlatformPrewarmCount);
            _isReady = true;
        }

        public PlatformController Get(PlatformGimmickType type)
        {
            if (!_isReady)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Get called before warmup.");
                return null;
            }

            var platform = _poolService.Get<PlatformController>(_configData.PlatformPoolKey);
            if (platform != null)
            {
                platform.ApplySprite(ResolvePlatformSprite(type));
            }

            return platform;
        }

        public void Release(PlatformController platform)
        {
            _poolService.Release(_configData.PlatformPoolKey, platform);
        }

        private PlatformController Create()
        {
            var platform = Object.Instantiate(_platformPrefab, _poolRoot);
            platform.Bind(_eventBus, _playerRegistry, _configData, _gameCamera);
            platform.InjectRelease(Release);
            return platform;
        }

        private Sprite ResolvePlatformSprite(PlatformGimmickType type)
        {
            return _resourceService.GetAsset<Sprite>(ResolvePlatformSpriteKey(type));
        }

        private string ResolvePlatformSpriteKey(PlatformGimmickType type)
        {
            switch (type)
            {
                case PlatformGimmickType.Shield:
                    return _configData.PlatformShieldSpriteAddressableKey;
                case PlatformGimmickType.Rocket:
                    return _configData.PlatformRocketSpriteAddressableKey;
                default:
                    return _configData.PlatformNormalSpriteAddressableKey;
            }
        }

        private void OnGet(PlatformController platform)
        {
            platform.gameObject.SetActive(true);
        }

        private void OnRelease(PlatformController platform)
        {
            platform.gameObject.SetActive(false);
        }
    }
}
