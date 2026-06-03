using System;
using JumJump.Controller;
using JumJump.Service;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Factory
{
    public sealed class PlatformFactory : IInitializable
    {
        private const int PrewarmCount = 12;
        private const string PoolKey = "Platform";

        private readonly PlatformController _platformPrefab;
        private readonly Transform _poolRoot;
        private readonly PoolService _poolService;

        public PlatformFactory(PlatformController platformPrefab, Transform poolRoot, PoolService poolService)
        {
            _platformPrefab = platformPrefab;
            _poolRoot = poolRoot;
            _poolService = poolService;
        }

        public void Initialize()
        {
            _poolService.Register(PoolKey, Create, OnGet, OnRelease, PrewarmCount);
        }

        public PlatformController Get()
        {
            return _poolService.Get<PlatformController>(PoolKey);
        }

        public void Release(PlatformController platform)
        {
            _poolService.Release(PoolKey, platform);
        }

        private PlatformController Create()
        {
            if (_platformPrefab == null)
            {
                Debug.LogError($"[{nameof(PlatformFactory)}] Missing platform prefab.");
                return null;
            }

            var platform = UnityEngine.Object.Instantiate(_platformPrefab, _poolRoot);
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
