using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class JumpBoxBoostFXService : IDisposable
    {
        private readonly EffectConfigData _configData;
        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;
        private Transform _poolRoot;
        private JumpBoxBoostFX _prefab;
        private int _createdCount;
        private bool _isReady;

        public JumpBoxBoostFXService(
            EffectConfigData configData,
            PoolService poolService,
            ResourceService resourceService)
        {
            _configData = configData;
            _poolService = poolService;
            _resourceService = resourceService;
        }

        public void Dispose()
        {
            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }

        public async UniTask WarmupAsync(CancellationToken cancellationToken = default)
        {
            if (_isReady)
            {
                return;
            }

            var loaded = await _resourceService.LoadKeyAsync(
                _configData.JumpBoxBoostFXAddressableKey,
                cancellationToken);
            if (!loaded)
            {
                GameLogger.Error(nameof(JumpBoxBoostFXService), $"Failed to load addressable FX: {_configData.JumpBoxBoostFXAddressableKey}");
                return;
            }

            var prefabObject = _resourceService.GetPrefab(_configData.JumpBoxBoostFXAddressableKey);
            if (prefabObject == null || !prefabObject.TryGetComponent(out _prefab))
            {
                GameLogger.Error(nameof(JumpBoxBoostFXService), $"Addressable prefab must have {nameof(JumpBoxBoostFX)}: {_configData.JumpBoxBoostFXAddressableKey}");
                return;
            }

            CreatePool();
            _isReady = true;
        }

        public void Play(Vector3 position)
        {
            if (!_isReady)
            {
                return;
            }

            var fx = _poolService.Get<JumpBoxBoostFX>(_configData.JumpBoxBoostFXPoolKey);
            fx.Play(position + _configData.JumpBoxBoostFXOffset);
        }

        private void CreatePool()
        {
            var root = new GameObject("FX_JumpBoxBoostPool");
            _poolRoot = root.transform;
            _poolService.Register(
                _configData.JumpBoxBoostFXPoolKey,
                CreateInstance,
                OnGet,
                OnRelease,
                Mathf.Max(1, _configData.JumpBoxBoostFXPoolCount));
        }

        private JumpBoxBoostFX CreateInstance()
        {
            var fx = UnityEngine.Object.Instantiate(_prefab, _poolRoot);
            fx.gameObject.name = $"{_configData.JumpBoxBoostFXPoolKey}_{_createdCount:00}";
            _createdCount++;
            fx.Bind(OnReturnedToPool);
            fx.SetPooled();
            return fx;
        }

        private void OnGet(JumpBoxBoostFX fx)
        {
            fx.SetPooled();
        }

        private void OnRelease(JumpBoxBoostFX fx)
        {
            fx.SetPooled();
        }

        private void OnReturnedToPool(JumpBoxBoostFX fx)
        {
            _poolService.Release(_configData.JumpBoxBoostFXPoolKey, fx);
        }
    }
}
