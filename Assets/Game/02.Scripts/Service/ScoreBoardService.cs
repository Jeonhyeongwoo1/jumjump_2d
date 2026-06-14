using System;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class ScoreBoardService : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;
        private readonly ResourceConfigData _resourceConfigData;
        private readonly BackgroundConfigData _backgroundConfigData;
        private readonly ScoreService _scoreService;
        private readonly Transform _poolRoot;

        private ScoreBoard _scoreBoardPrefab;
        private ScoreBoard _activeScoreBoard;
        private PlatformController _activePlatform;
        private bool _isReady;

        public ScoreBoardService(
            IEventBus eventBus,
            PoolService poolService,
            ResourceService resourceService,
            ResourceConfigData resourceConfigData,
            BackgroundConfigData backgroundConfigData,
            ScoreService scoreService,
            Transform poolRoot)
        {
            _eventBus = eventBus;
            _poolService = poolService;
            _resourceService = resourceService;
            _resourceConfigData = resourceConfigData;
            _backgroundConfigData = backgroundConfigData;
            _scoreService = scoreService;
            _poolRoot = poolRoot;
        }

        public void Warmup()
        {
            if (_isReady)
            {
                return;
            }

            var prefab = _resourceService.GetPrefab(_resourceConfigData.ScoreBoardAddressableKey);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(ScoreBoardService)}] Failed to load prefab: {_resourceConfigData.ScoreBoardAddressableKey}");
                return;
            }

            _scoreBoardPrefab = prefab.GetComponent<ScoreBoard>();
            _poolService.Register(
                _resourceConfigData.ScoreBoardPoolKey,
                Create,
                view => view.gameObject.SetActive(true),
                view => view.Hide(),
                _resourceConfigData.ScoreBoardPrewarmCount);

            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<PlatformShieldBlockedEvent>(OnPlatformShieldBlocked);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            _isReady = true;
        }

        public void TryShowForPlatform(PlatformController platform, Vector3 landingTargetPosition)
        {
            if (!_isReady || platform == null || _activeScoreBoard != null ||
                !_scoreService.ShouldPreviewHighScoreBoardOnNextLanding())
            {
                return;
            }

            var sprite = ResolveSprite(landingTargetPosition.y);
            if (sprite == null)
            {
                return;
            }

            var view = _poolService.Get<ScoreBoard>(_resourceConfigData.ScoreBoardPoolKey);
            if (view == null)
            {
                return;
            }

            _activeScoreBoard = view;
            _activePlatform = platform;
            view.Show(landingTargetPosition, sprite);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<PlatformShieldBlockedEvent>(OnPlatformShieldBlocked);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
            ReleaseActive();
        }

        private ScoreBoard Create()
        {
            return UnityEngine.Object.Instantiate(_scoreBoardPrefab, _poolRoot);
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            ReleaseForPlatform(ev.Platform);
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            ReleaseActive();
        }

        private void OnPlatformShieldBlocked(in PlatformShieldBlockedEvent ev)
        {
            ReleaseForPlatform(ev.Platform);
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            ReleaseActive();
        }

        public void ReleaseForPlatform(PlatformController platform)
        {
            if (platform == null || platform != _activePlatform)
            {
                return;
            }

            ReleaseActive();
        }

        private void ReleaseActive()
        {
            if (_activeScoreBoard == null)
            {
                _activePlatform = null;
                return;
            }

            _poolService.Release(_resourceConfigData.ScoreBoardPoolKey, _activeScoreBoard);
            _activeScoreBoard = null;
            _activePlatform = null;
        }

        private Sprite ResolveSprite(float height)
        {
            var keys = _resourceConfigData.ScoreBoardSpriteAddressableKeys;
            if (keys == null || keys.Length == 0)
            {
                return null;
            }

            var index = Mathf.Clamp(ResolveDepthLayerIndex(height), 0, keys.Length - 1);
            return _resourceService.GetAsset<Sprite>(keys[index]);
        }

        private int ResolveDepthLayerIndex(float height)
        {
            var layers = _backgroundConfigData.BackgroundDepthLayers;
            if (layers == null || layers.Length == 0)
            {
                return 0;
            }

            var selected = 0;
            for (var i = 0; i < layers.Length; i++)
            {
                if (height >= layers[i].StartHeight)
                {
                    selected = i;
                }
            }

            return selected;
        }
    }
}
