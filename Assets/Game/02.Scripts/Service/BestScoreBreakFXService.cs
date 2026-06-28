using System;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class BestScoreBreakFXService : IInitializable, IDisposable
    {
        private readonly EffectConfigData _configData;
        private readonly IEventBus _eventBus;
        private readonly PlayerRegistry _playerRegistry;
        private readonly PoolService _poolService;
        private readonly ScoreService _scoreService;

        private Transform _poolRoot;
        private int _createdCount;
        private bool _hasPlayedForRound;
        private bool _isReady;

        [Inject]
        public BestScoreBreakFXService(
            EffectConfigData configData,
            IEventBus eventBus,
            PlayerRegistry playerRegistry,
            PoolService poolService,
            ScoreService scoreService)
        {
            _configData = configData;
            _eventBus = eventBus;
            _playerRegistry = playerRegistry;
            _poolService = poolService;
            _scoreService = scoreService;
        }

        public void Initialize()
        {
            CreatePool();
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);

            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            if (!_isReady || _hasPlayedForRound || ev.Score <= _scoreService.RoundHighScoreTarget)
            {
                return;
            }

            _hasPlayedForRound = true;
            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            Play(player.Position + _configData.BestScoreBreakFXOffset);
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            _hasPlayedForRound = false;
        }

        private void Play(Vector3 position)
        {
            var fx = _poolService.Get<BestScoreBreakFX>(_configData.BestScoreBreakFXPoolKey);
            if (fx == null)
            {
                return;
            }

            fx.Play(position);
        }

        private void CreatePool()
        {
            var root = new GameObject("FX_BestScoreBreakPool");
            _poolRoot = root.transform;
            _poolService.Register(
                _configData.BestScoreBreakFXPoolKey,
                CreateInstance,
                OnGet,
                OnRelease,
                Mathf.Max(1, _configData.BestScoreBreakFXPoolCount));
            _isReady = true;
        }

        private BestScoreBreakFX CreateInstance()
        {
            var fx = UnityEngine.Object.Instantiate(_configData.BestScoreBreakFXPrefab, _poolRoot);
            fx.gameObject.name = $"{_configData.BestScoreBreakFXPoolKey}_{_createdCount:00}";
            _createdCount++;
            fx.Bind(OnReturnedToPool);
            fx.SetPooled();
            return fx;
        }

        private void OnGet(BestScoreBreakFX fx)
        {
            fx.SetPooled();
        }

        private void OnRelease(BestScoreBreakFX fx)
        {
            fx.SetPooled();
        }

        private void OnReturnedToPool(BestScoreBreakFX fx)
        {
            _poolService.Release(_configData.BestScoreBreakFXPoolKey, fx);
        }
    }
}
