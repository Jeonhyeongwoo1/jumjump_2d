using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class CoinCollectFXService : IInitializable, IDisposable
    {
        private readonly EffectConfigData _configData;
        private readonly IEventBus _eventBus;
        private readonly PlayerRegistry _playerRegistry;
        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;
        private Transform _poolRoot;
        private CoinCollectFX _prefab;
        private int _createdCount;
        private bool _isReady;

        public CoinCollectFXService(
            EffectConfigData configData,
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            PoolService poolService,
            ResourceService resourceService)
        {
            _configData = configData;
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _poolService = poolService;
            _resourceService = resourceService;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Subscribe<BestScoreReachedEvent>(OnBestScoreReached);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Unsubscribe<BestScoreReachedEvent>(OnBestScoreReached);

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
                _configData.CoinCollectFXAddressableKey,
                cancellationToken);
            if (!loaded)
            {
                GameLogger.Error(nameof(CoinCollectFXService), $"Failed to load addressable FX: {_configData.CoinCollectFXAddressableKey}");
                return;
            }

            var prefabObject = _resourceService.GetPrefab(_configData.CoinCollectFXAddressableKey);
            if (prefabObject == null || !prefabObject.TryGetComponent(out _prefab))
            {
                GameLogger.Error(nameof(CoinCollectFXService), $"Addressable prefab must have {nameof(CoinCollectFX)}: {_configData.CoinCollectFXAddressableKey}");
                return;
            }

            CreatePool();
            _isReady = true;
        }

        private void OnGoldChanged(in GoldChangedEvent ev)
        {
            if (ev.GoldDelta <= 0 || !_isReady)
            {
                return;
            }

            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            Play(player.Position + _configData.CoinCollectFXOffset);
        }

        private void OnBestScoreReached(in BestScoreReachedEvent ev)
        {
            if (!_isReady)
            {
                return;
            }

            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            Play(player.Position + _configData.CoinCollectFXOffset);
        }

        private void Play(Vector3 position)
        {
            var fx = _poolService.Get<CoinCollectFX>(_configData.CoinCollectFXPoolKey);
            if (fx == null)
            {
                return;
            }

            fx.Play(position);
        }

        private void CreatePool()
        {
            var root = new GameObject("FX_CoinCollectPool");
            _poolRoot = root.transform;
            _poolService.Register(
                _configData.CoinCollectFXPoolKey,
                CreateInstance,
                OnGet,
                OnRelease,
                Mathf.Max(1, _configData.CoinCollectFXPoolCount));
        }

        private CoinCollectFX CreateInstance()
        {
            var fx = UnityEngine.Object.Instantiate(_prefab, _poolRoot);
            fx.gameObject.name = $"{_configData.CoinCollectFXPoolKey}_{_createdCount:00}";
            _createdCount++;
            fx.Bind(OnReturnedToPool);
            fx.SetPooled();
            return fx;
        }

        private void OnGet(CoinCollectFX fx)
        {
            fx.SetPooled();
        }

        private void OnRelease(CoinCollectFX fx)
        {
            fx.SetPooled();
        }

        private void OnReturnedToPool(CoinCollectFX fx)
        {
            _poolService.Release(_configData.CoinCollectFXPoolKey, fx);
        }
    }
}
