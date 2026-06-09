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
        private readonly ResourceConfigData _resourceConfigData;
        private readonly PlatformConfigData _platformConfigData;
        private readonly PlayerConfigData _playerConfigData;
        private readonly UnityEngine.Camera _gameCamera;

        private PlatformController _platformPrefab;
        private bool _isReady;

        public PlatformFactory(
            Transform poolRoot,
            PoolService poolService,
            ResourceService resourceService,
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            ResourceConfigData resourceConfigData,
            PlatformConfigData platformConfigData,
            PlayerConfigData playerConfigData,
            UnityEngine.Camera gameCamera)
        {
            _poolRoot = poolRoot;
            _poolService = poolService;
            _resourceService = resourceService;
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _resourceConfigData = resourceConfigData;
            _platformConfigData = platformConfigData;
            _playerConfigData = playerConfigData;
            _gameCamera = gameCamera;
        }

        public void Warmup()
        {
            if (_isReady)
            {
                return;
            }

            var prefab = _resourceService.GetPrefab(_resourceConfigData.PlatformAddressableKey);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Failed to load platform prefab: {_resourceConfigData.PlatformAddressableKey}");
                return;
            }

            _platformPrefab = prefab.GetComponent<PlatformController>();
            _poolService.Register(_resourceConfigData.PlatformPoolKey, Create, OnGet, OnRelease, _resourceConfigData.PlatformPrewarmCount);
            _isReady = true;
        }

        public PlatformController Get(PlatformGimmickType type)
        {
            if (!_isReady)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Get called before warmup.");
                return null;
            }

            var platform = _poolService.Get<PlatformController>(_resourceConfigData.PlatformPoolKey);
            if (platform != null)
            {
                platform.ApplySprite(ResolvePlatformSprite(type));
            }

            return platform;
        }

        public void Release(PlatformController platform)
        {
            _poolService.Release(_resourceConfigData.PlatformPoolKey, platform);
        }

        private PlatformController Create()
        {
            var platform = Object.Instantiate(_platformPrefab, _poolRoot);
            platform.Bind(_eventBus, _playerRegistry, _platformConfigData, _playerConfigData, _gameCamera);
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
                    return _platformConfigData.PlatformShieldSpriteAddressableKey;
                case PlatformGimmickType.Rocket:
                    return _platformConfigData.PlatformRocketSpriteAddressableKey;
                default:
                    return _platformConfigData.PlatformNormalSpriteAddressableKey;
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
