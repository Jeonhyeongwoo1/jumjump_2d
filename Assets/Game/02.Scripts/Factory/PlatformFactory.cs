using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Controller;
using JumJump.Service;
using UnityEngine;

namespace JumJump.Factory
{
    public sealed class PlatformFactory
    {
        public const string AddressableKey = "Platform";

        private const int PrewarmCount = 12;
        private const string PoolKey = "Platform";

        private readonly Transform _poolRoot;
        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;

        private PlatformController _platformPrefab;
        private bool _isReady;

        public PlatformFactory(Transform poolRoot, PoolService poolService, ResourceService resourceService)
        {
            _poolRoot = poolRoot;
            _poolService = poolService;
            _resourceService = resourceService;
        }

        public async UniTask WarmupAsync(CancellationToken cancellationToken = default)
        {
            if (_isReady)
            {
                return;
            }

            var prefab = await _resourceService.LoadPrefabAsync(AddressableKey, cancellationToken);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Failed to load platform prefab: {AddressableKey}");
                return;
            }

            _platformPrefab = prefab.GetComponent<PlatformController>();
            if (_platformPrefab == null)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Loaded prefab has no {nameof(PlatformController)}.");
                return;
            }

            _poolService.Register(PoolKey, Create, OnGet, OnRelease, PrewarmCount);
            _isReady = true;
        }

        public PlatformController Get()
        {
            if (!_isReady)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Get called before warmup.");
                return null;
            }

            return _poolService.Get<PlatformController>(PoolKey);
        }

        public void Release(PlatformController platform)
        {
            _poolService.Release(PoolKey, platform);
        }

        private PlatformController Create()
        {
            var platform = Object.Instantiate(_platformPrefab, _poolRoot);
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
