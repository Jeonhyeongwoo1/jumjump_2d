using System;
using System.Collections.Generic;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class ComboPlatformFXService : IInitializable, ITickable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly EffectConfigData _configData;
        private readonly PoolService _poolService;
        private readonly ComboPlatformFXFactory _factory;
        private readonly List<ComboPlatformAuraFX> _activeAuras = new List<ComboPlatformAuraFX>(16);
        private readonly List<PlatformController> _comboPlatforms = new List<PlatformController>(16);

        private Transform _poolRoot;
        private int _auraCreatedCount;
        private int _burstCreatedCount;
        private int _pendingPlatformPulseIndex;
        private int _pendingPlatformPulseComboCount;
        private bool _hasPendingPlatformPulse;

        public ComboPlatformFXService(
            IEventBus eventBus,
            EffectConfigData configData,
            PoolService poolService,
            ComboPlatformFXFactory factory)
        {
            _eventBus = eventBus;
            _configData = configData;
            _poolService = poolService;
            _factory = factory;
        }

        public void Initialize()
        {
            CreatePools();
            _eventBus.Subscribe<ComboPlatformActivatedEvent>(OnComboPlatformActivated);
            _eventBus.Subscribe<ComboEndedEvent>(OnComboEnded);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        public void Tick()
        {
            if (!_hasPendingPlatformPulse)
            {
                return;
            }

            PlayNextPendingPlatformPulse();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ComboPlatformActivatedEvent>(OnComboPlatformActivated);
            _eventBus.Unsubscribe<ComboEndedEvent>(OnComboEnded);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);

            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }

        private void OnComboPlatformActivated(in ComboPlatformActivatedEvent ev)
        {
            if (ev.IsComboStarted)
            {
                FadeAllAuras();
                _comboPlatforms.Clear();
                StopPendingPlatformPulses();
            }

            AddComboPlatform(ev.Platform);
            PlayPlatformPulse(ev.Platform, ev.ComboCount);
            ScheduleComboPlatformPulseWave(ev.ComboCount);

            var position = ResolveFXPosition(ev);
            PlayAura(position, ev.ComboCount);
            PlayBurst(position, ev.ComboCount);
        }

        private void OnComboEnded(in ComboEndedEvent ev)
        {
            ClearCombo();
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            ClearCombo();
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            ClearCombo();
        }

        private void CreatePools()
        {
            _poolRoot = _factory.CreateRoot();
            _poolService.Register(
                _configData.ComboPlatformAuraFXPoolKey,
                CreateAuraInstance,
                OnAuraGet,
                OnAuraRelease,
                Mathf.Max(1, _configData.ComboPlatformAuraFXPoolCount));
            _poolService.Register(
                _configData.ComboPlatformBurstFXPoolKey,
                CreateBurstInstance,
                OnBurstGet,
                OnBurstRelease,
                Mathf.Max(1, _configData.ComboPlatformBurstFXPoolCount));
        }

        private void PlayAura(Vector3 position, int comboCount)
        {
            var aura = _poolService.Get<ComboPlatformAuraFX>(_configData.ComboPlatformAuraFXPoolKey);
            if (aura == null)
            {
                return;
            }

            aura.Play(position, comboCount);
            _activeAuras.Add(aura);
            TrimActiveAuras();
        }

        private void PlayBurst(Vector3 position, int comboCount)
        {
            var burst = _poolService.Get<ComboPlatformBurstFX>(_configData.ComboPlatformBurstFXPoolKey);
            if (burst == null)
            {
                return;
            }

            burst.Play(position, comboCount);
        }

        private void TrimActiveAuras()
        {
            var maxCount = Mathf.Max(1, _configData.ComboPlatformAuraMaxActiveCount);
            while (_activeAuras.Count > maxCount)
            {
                var aura = _activeAuras[0];
                _activeAuras.RemoveAt(0);
                aura.FadeOut();
            }
        }

        private void FadeAllAuras()
        {
            for (var i = 0; i < _activeAuras.Count; i++)
            {
                _activeAuras[i].FadeOut();
            }

            _activeAuras.Clear();
        }

        private void AddComboPlatform(PlatformController platform)
        {
            if (platform == null || _comboPlatforms.Contains(platform))
            {
                return;
            }

            _comboPlatforms.Add(platform);
        }

        private void ScheduleComboPlatformPulseWave(int comboCount)
        {
            _pendingPlatformPulseIndex = _comboPlatforms.Count - 2;
            _pendingPlatformPulseComboCount = comboCount;
            _hasPendingPlatformPulse = _pendingPlatformPulseIndex >= 0;
        }

        private void PlayNextPendingPlatformPulse()
        {
            while (_pendingPlatformPulseIndex >= 0)
            {
                var platform = _comboPlatforms[_pendingPlatformPulseIndex];
                _pendingPlatformPulseIndex--;
                if (platform == null)
                {
                    continue;
                }

                if (!platform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                PlayPlatformPulse(platform, _pendingPlatformPulseComboCount);
                return;
            }

            StopPendingPlatformPulses();
        }

        private void ClearCombo()
        {
            FadeAllAuras();
            _comboPlatforms.Clear();
            StopPendingPlatformPulses();
        }

        private void StopPendingPlatformPulses()
        {
            _pendingPlatformPulseIndex = -1;
            _pendingPlatformPulseComboCount = 0;
            _hasPendingPlatformPulse = false;
        }

        private void PlayPlatformPulse(PlatformController platform, int comboCount)
        {
            if (platform == null || !platform.gameObject.activeInHierarchy)
            {
                return;
            }

            platform.PlayComboPulse(comboCount);
        }

        private Vector3 ResolveFXPosition(in ComboPlatformActivatedEvent ev)
        {
            return new Vector3(
                ev.Platform.CenterX,
                ev.Platform.TopY,
                ev.LandingPosition.z) + _configData.ComboPlatformFXOffset;
        }

        private ComboPlatformAuraFX CreateAuraInstance()
        {
            var fx = _factory.CreateAura(_poolRoot, OnAuraReturnedToPool);
            fx.gameObject.name = $"{_configData.ComboPlatformAuraFXPoolKey}_{_auraCreatedCount:00}";
            _auraCreatedCount++;
            fx.SetPooled();
            return fx;
        }

        private ComboPlatformBurstFX CreateBurstInstance()
        {
            var fx = _factory.CreateBurst(_poolRoot, OnBurstReturnedToPool);
            fx.gameObject.name = $"{_configData.ComboPlatformBurstFXPoolKey}_{_burstCreatedCount:00}";
            _burstCreatedCount++;
            fx.SetPooled();
            return fx;
        }

        private void OnAuraGet(ComboPlatformAuraFX fx)
        {
            fx.SetPooled();
        }

        private void OnAuraRelease(ComboPlatformAuraFX fx)
        {
            fx.SetPooled();
        }

        private void OnBurstGet(ComboPlatformBurstFX fx)
        {
            fx.SetPooled();
        }

        private void OnBurstRelease(ComboPlatformBurstFX fx)
        {
            fx.SetPooled();
        }

        private void OnAuraReturnedToPool(ComboPlatformAuraFX fx)
        {
            _poolService.Release(_configData.ComboPlatformAuraFXPoolKey, fx);
        }

        private void OnBurstReturnedToPool(ComboPlatformBurstFX fx)
        {
            _poolService.Release(_configData.ComboPlatformBurstFXPoolKey, fx);
        }
    }
}
