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

        private PlatformController _platformPrefab;
        private UnityEngine.Camera _gameCamera;
        private bool _isReady;

        public PlatformFactory(
            Transform poolRoot,
            PoolService poolService,
            ResourceService resourceService,
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            GameConfigData configData)
        {
            _poolRoot = poolRoot;
            _poolService = poolService;
            _resourceService = resourceService;
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _configData = configData;
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
            if (_platformPrefab == null)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Loaded prefab has no {nameof(PlatformController)}.");
                return;
            }

            _gameCamera = UnityEngine.Camera.main;
            if (_gameCamera == null)
            {
                Debug.LogWarning($"[{nameof(PlatformFactory)}] Main camera not found; camera-aware platform gimmicks may not run.");
            }

            _poolService.Register(_configData.PlatformPoolKey, Create, OnGet, OnRelease, _configData.PlatformPrewarmCount);
            _isReady = true;
        }

        public PlatformController Get()
        {
            if (!_isReady)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Get called before warmup.");
                return null;
            }

            return _poolService.Get<PlatformController>(_configData.PlatformPoolKey);
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
