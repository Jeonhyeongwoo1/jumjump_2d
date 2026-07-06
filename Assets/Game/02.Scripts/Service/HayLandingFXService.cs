using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class HayLandingFXService : IInitializable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly EffectConfigData _configData;
        private readonly PoolService _poolService;
        private Transform _poolRoot;
        private int _createdCount;

        [Inject]
        public HayLandingFXService(
            IEventBus eventBus,
            EffectConfigData configData,
            PoolService poolService)
        {
            _eventBus = eventBus;
            _configData = configData;
            _poolService = poolService;
        }

        public void Initialize()
        {
            CreatePool();
            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);

            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }

        public void Play(Vector3 position, bool includePerfectTwinkle)
        {
            var fx = _poolService.Get<HayLandingFX>(_configData.HayLandingFXPoolKey);
            fx.Play(position, includePerfectTwinkle);
        }

        private void CreatePool()
        {
            var root = new GameObject("FX_HayLandingPool");
            _poolRoot = root.transform;
            _poolService.Register(
                _configData.HayLandingFXPoolKey,
                CreateInstance,
                OnGet,
                OnRelease,
                Mathf.Max(1, _configData.HayLandingFXPoolCount));
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            Play(ev.LandingPosition + _configData.HayLandingFXOffset, false);
        }

        private HayLandingFX CreateInstance()
        {
            var fx = UnityEngine.Object.Instantiate(_configData.HayLandingFXPrefab, _poolRoot);
            fx.gameObject.name = $"{_configData.HayLandingFXPoolKey}_{_createdCount:00}";
            _createdCount++;
            fx.Bind(OnReturnedToPool);
            fx.SetPooled();
            return fx;
        }

        private void OnGet(HayLandingFX fx)
        {
            fx.SetPooled();
        }

        private void OnRelease(HayLandingFX fx)
        {
            fx.SetPooled();
        }

        private void OnReturnedToPool(HayLandingFX fx)
        {
            _poolService.Release(_configData.HayLandingFXPoolKey, fx);
        }
    }
}
