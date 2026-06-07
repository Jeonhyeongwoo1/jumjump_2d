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
        private readonly PlatformCheatData _platformCheatData;

        public PlatformSpawnService(
            IEventBus eventBus,
            PlatformFactory platformFactory,
            PlatformRegistry platformRegistry,
            PlayerRegistry playerRegistry,
            ScoreService scoreService,
            GameConfigData configData,
            PlatformCheatData platformCheatData)
        {
            _eventBus = eventBus;
            _platformFactory = platformFactory;
            _platformRegistry = platformRegistry;
            _playerRegistry = playerRegistry;
            _scoreService = scoreService;
            _configData = configData;
            _platformCheatData = platformCheatData;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        private void ResetRound()
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Player not ready; cannot reset round.");
                return;
            }

            _platformRegistry.Clear();
            _nextPlatformIndex = 0;
            player.ResetForRound();
        }

        private PlatformController SpawnIncomingPlatform()
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Player not ready; cannot spawn platform.");
                return null;
            }

            var platformY = player.Position.y;
            return SpawnIncomingPlatformAtY(platformY);
        }

        private PlatformController SpawnIncomingPlatformAtY(float platformY)
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Player not ready; cannot spawn platform.");
                return null;
            }

            var targetX = player.Position.x;
            var spawnSide = ResolveSpawnSide();
            var spawnX = targetX + spawnSide * Mathf.Max(0f, _configData.PlatformSpawnDistance);
            var scoreFactor = Mathf.Clamp01(_scoreService.Score / Mathf.Max(1f, _configData.PlatformScoreSpeedMaxScore));
            var moveSpeed = Mathf.Max(
                0f,
                _configData.PlatformBaseMoveSpeed + scoreFactor * _configData.PlatformMaxMoveSpeedBonus);

            return SpawnPlatform(new Vector3(spawnX, platformY, 0f), targetX, moveSpeed);
        }

        private PlatformController SpawnPlatform(Vector3 position, float targetX, float moveSpeed)
        {
            var platform = _platformFactory.Get();
            if (platform == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Failed to get platform from factory.");
                return null;
            }

            var gimmickSetting = ResolvePlatformGimmickSetting();
            var platformWidthScale = ResolvePlatformWidthScale(gimmickSetting);
            var platformMoveSpeed = moveSpeed * ResolvePlatformMoveSpeedScale(gimmickSetting);
            platform.Initialize(
                _nextPlatformIndex,
                gimmickSetting == null ? PlatformGimmickType.Normal : gimmickSetting.Type,
                position,
                targetX,
                platformWidthScale,
                1f,
                _configData.PlatformLandingHeight,
                platformMoveSpeed);
            _platformRegistry.Register(platform);
            _nextPlatformIndex++;
            return platform;
        }

        private PlatformGimmickSetting ResolvePlatformGimmickSetting()
        {
            var settings = _configData.PlatformGimmickSettings;
            var normalSetting = FindPlatformGimmickSetting(settings, PlatformGimmickType.Normal);
            if (_platformCheatData != null && _platformCheatData.ForcePlatformGimmick)
            {
                var forcedSetting = FindPlatformGimmickSetting(settings, _platformCheatData.ForcedPlatformGimmickType);
                return forcedSetting ?? normalSetting;
            }

            if (settings == null || settings.Length == 0)
            {
                return normalSetting;
            }

            var totalChance = 0f;
            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (setting == null || setting.Type == PlatformGimmickType.Normal)
                {
                    continue;
                }

                if (_scoreService.Score <= setting.StartScore)
                {
                    continue;
                }

                totalChance += Mathf.Clamp01(setting.SpawnChance);
            }

            if (totalChance <= 0f)
            {
                return normalSetting;
            }

            var roll = totalChance <= 1f
                ? UnityEngine.Random.value
                : UnityEngine.Random.Range(0f, totalChance);
            var cumulativeChance = 0f;
            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (setting == null || setting.Type == PlatformGimmickType.Normal)
                {
                    continue;
                }

                if (_scoreService.Score <= setting.StartScore)
                {
                    continue;
                }

                cumulativeChance += Mathf.Clamp01(setting.SpawnChance);
                if (roll <= cumulativeChance)
                {
                    return setting;
                }
            }

            return normalSetting;
        }

        private PlatformGimmickSetting FindPlatformGimmickSetting(
            PlatformGimmickSetting[] settings,
            PlatformGimmickType type)
        {
            if (settings == null)
            {
                return null;
            }

            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (setting != null && setting.Type == type)
                {
                    return setting;
                }
            }

            return null;
        }

        private float ResolvePlatformWidthScale(PlatformGimmickSetting setting)
        {
            if (setting == null)
            {
                return 1f;
            }

            var minWidthScale = Mathf.Clamp01(setting.MinWidthScale);
            var maxWidthScale = Mathf.Clamp01(setting.MaxWidthScale);
            if (maxWidthScale < minWidthScale)
            {
                maxWidthScale = minWidthScale;
            }

            var widthScale = UnityEngine.Random.Range(minWidthScale, maxWidthScale);
            return Mathf.Max(0.01f, widthScale);
        }

        private float ResolvePlatformMoveSpeedScale(PlatformGimmickSetting setting)
        {
            if (setting == null)
            {
                return 1f;
            }

            var minMoveSpeedScale = Mathf.Max(0f, setting.MinMoveSpeedScale);
            var maxMoveSpeedScale = Mathf.Max(0f, setting.MaxMoveSpeedScale);
            if (maxMoveSpeedScale < minMoveSpeedScale)
            {
                maxMoveSpeedScale = minMoveSpeedScale;
            }

            var moveSpeedScale = UnityEngine.Random.Range(minMoveSpeedScale, maxMoveSpeedScale);
            return Mathf.Max(0f, moveSpeedScale);
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            if (ev.Platform == null)
            {
                return;
            }

            _platformRegistry.ReleaseBelow(ev.Platform.CenterY - Mathf.Max(0f, _configData.PlatformCleanupBelowDistance));
            SpawnIncomingPlatformAtY(ev.Platform.GetStackedNextCenterY());
        }

        private float ResolveSpawnSide()
        {
            var firstDirection = _configData.PlatformFirstSpawnDirection >= 0 ? 1f : -1f;
            if (!_configData.PlatformAlternatesSpawnSide)
            {
                return firstDirection;
            }

            return _nextPlatformIndex % 2 == 0 ? firstDirection : -firstDirection;
        }

        private void OnResourcesReady(in GameResourcesReadyEvent ev)
        {
            ResetRound();
        }

        private void OnGameStarted(in GameStartedEvent ev)
        {
            SpawnIncomingPlatform();
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            ResetRound();
        }
    }
}
