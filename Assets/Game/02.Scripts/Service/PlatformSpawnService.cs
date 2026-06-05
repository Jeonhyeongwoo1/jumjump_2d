using System;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class PlatformSpawnService : IInitializable, IDisposable
    {
        private int _nextPlatformIndex;
        private readonly IEventBus _eventBus;
        private readonly PlatformFactory _platformFactory;
        private readonly PlatformRegistry _platformRegistry;
        private readonly PlayerRegistry _playerRegistry;
        private readonly ScoreService _scoreService;
        private readonly GameConfigData _configData;

        public PlatformSpawnService(
            IEventBus eventBus,
            PlatformFactory platformFactory,
            PlatformRegistry platformRegistry,
            PlayerRegistry playerRegistry,
            ScoreService scoreService,
            GameConfigData configData)
        {
            _eventBus = eventBus;
            _platformFactory = platformFactory;
            _platformRegistry = platformRegistry;
            _playerRegistry = playerRegistry;
            _scoreService = scoreService;
            _configData = configData;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        private void ResetPlatforms()
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Player not ready; cannot reset platforms.");
                return;
            }

            _platformRegistry.Clear();
            _nextPlatformIndex = 0;

            var startPlatform = SpawnPlatform(Vector3.zero, 0f);
            player.PlaceOnPlatform(startPlatform);

            for (var i = 1; i < _configData.InitialPlatformCount; i++)
            {
                SpawnNextPlatform();
            }

            _eventBus.Publish(new PlatformsResetEvent(startPlatform));
        }

        private PlatformController SpawnNextPlatform()
        {
            var nextY = _nextPlatformIndex * _configData.PlatformVerticalSpacing;
            var xPatternModulo = Mathf.Max(1, _configData.PlatformXPatternModulo);
            var xStep = ((_nextPlatformIndex * _configData.PlatformXPatternMultiplier) % xPatternModulo) /
                        (float)xPatternModulo;
            var x = Mathf.Lerp(-_configData.PlatformXRange, _configData.PlatformXRange, xStep);
            var scoreFactor = Mathf.Clamp01(_scoreService.Score / Mathf.Max(1f, _configData.PlatformScoreSpeedMaxScore));
            var moveSpeed = _nextPlatformIndex < _configData.StationaryPlatformCount
                ? 0f
                : _configData.PlatformBaseMoveSpeed + scoreFactor;
            return SpawnPlatform(new Vector3(x, nextY, 0f), moveSpeed);
        }

        private PlatformController SpawnPlatform(Vector3 position, float moveSpeed)
        {
            var platform = _platformFactory.Get();
            if (platform == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Failed to get platform from factory.");
                return null;
            }

            platform.Initialize(
                _nextPlatformIndex,
                position,
                _configData.PlatformWidth,
                _configData.PlatformHeight,
                _configData.PlatformLandingHeight,
                moveSpeed,
                -_configData.PlatformXRange,
                _configData.PlatformXRange);
            _platformRegistry.Register(platform);
            _nextPlatformIndex++;
            return platform;
        }

        private void EnsurePlatformsAhead(PlatformController landedPlatform)
        {
            if (landedPlatform == null)
            {
                return;
            }

            while (_nextPlatformIndex - landedPlatform.PlatformIndex <= _configData.PlatformsAhead)
            {
                if (SpawnNextPlatform() == null)
                {
                    return;
                }
            }
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            if (ev.Platform == null)
            {
                return;
            }

            _platformRegistry.ReleaseBelow(ev.Platform.CenterY);
            EnsurePlatformsAhead(ev.Platform);
        }

        private void OnResourcesReady(in GameResourcesReadyEvent ev)
        {
            ResetPlatforms();
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            ResetPlatforms();
        }
    }
}
