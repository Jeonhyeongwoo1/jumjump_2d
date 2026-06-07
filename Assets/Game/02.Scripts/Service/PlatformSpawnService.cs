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
    public sealed class PlatformSpawnService : IInitializable, ITickable, IDisposable
    {
        private int _nextPlatformIndex;
        private float _pendingDoublePreSpawnElapsed;
        private float _pendingDoublePreSpawnDelay;
        private float _pendingDoublePlatformY;
        private bool _hasPendingDoublePreSpawn;
        private PlatformController _doubleSourcePlatform;
        private PlatformController _prefetchedDoublePlatform;
        private readonly IEventBus _eventBus;
        private readonly PlatformFactory _platformFactory;
        private readonly PlatformRegistry _platformRegistry;
        private readonly PlayerRegistry _playerRegistry;
        private readonly ScoreService _scoreService;
        private readonly PlatformGimmickBehaviourFactory _platformGimmickBehaviourFactory;
        private readonly GameConfigData _configData;
        private readonly PlatformCheatData _platformCheatData;

        public PlatformSpawnService(
            IEventBus eventBus,
            PlatformFactory platformFactory,
            PlatformRegistry platformRegistry,
            PlayerRegistry playerRegistry,
            ScoreService scoreService,
            PlatformGimmickBehaviourFactory platformGimmickBehaviourFactory,
            GameConfigData configData,
            PlatformCheatData platformCheatData)
        {
            _eventBus = eventBus;
            _platformFactory = platformFactory;
            _platformRegistry = platformRegistry;
            _playerRegistry = playerRegistry;
            _scoreService = scoreService;
            _platformGimmickBehaviourFactory = platformGimmickBehaviourFactory;
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

        public void Tick()
        {
            if (!_hasPendingDoublePreSpawn)
            {
                return;
            }

            _pendingDoublePreSpawnElapsed += Time.deltaTime;
            if (_pendingDoublePreSpawnElapsed < _pendingDoublePreSpawnDelay)
            {
                return;
            }

            SpawnPendingDoublePlatform();
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
            ClearPendingDoubleSpawn();
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
            var moveSpeed = ResolveBaseMoveSpeed();
            var gimmickSetting = ResolvePlatformGimmickSetting();
            if (gimmickSetting != null && gimmickSetting.Type == PlatformGimmickType.Double)
            {
                var normalSetting = FindPlatformGimmickSetting(
                    _configData.PlatformGimmickSettings,
                    PlatformGimmickType.Normal);
                var platform = SpawnPlatform(new Vector3(spawnX, platformY, 0f), targetX, moveSpeed, normalSetting);
                ScheduleDoublePreSpawn(platform);
                return platform;
            }

            return SpawnPlatform(new Vector3(spawnX, platformY, 0f), targetX, moveSpeed, gimmickSetting);
        }

        private void ScheduleDoublePreSpawn(PlatformController sourcePlatform)
        {
            if (sourcePlatform == null)
            {
                ClearPendingDoubleSpawn();
                return;
            }

            _doubleSourcePlatform = sourcePlatform;
            _pendingDoublePlatformY = sourcePlatform.GetStackedNextCenterY();
            _pendingDoublePreSpawnDelay = Mathf.Max(0f, _configData.PlatformDoublePreSpawnDelay);
            _pendingDoublePreSpawnElapsed = 0f;
            _hasPendingDoublePreSpawn = true;

            if (_pendingDoublePreSpawnDelay <= 0f)
            {
                SpawnPendingDoublePlatform();
            }
        }

        private void SpawnPendingDoublePlatform()
        {
            if (!_hasPendingDoublePreSpawn)
            {
                return;
            }

            _hasPendingDoublePreSpawn = false;

            var player = _playerRegistry.Player;
            if (player == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Player not ready; cannot spawn pending double platform.");
                return;
            }

            var targetX = player.Position.x;
            var spawnSide = ResolveSpawnSide();
            var spawnX = targetX + spawnSide * Mathf.Max(0f, _configData.PlatformSpawnDistance);
            var moveSpeed = ResolveBaseMoveSpeed();
            var gimmickSetting = ResolveDoubleFollowUpGimmickSetting();
            _prefetchedDoublePlatform = SpawnPlatform(
                new Vector3(spawnX, _pendingDoublePlatformY, 0f),
                targetX,
                moveSpeed,
                gimmickSetting);
            _prefetchedDoublePlatform?.SetInteractionEnabled(false);
        }

        private PlatformController SpawnPlatform(
            Vector3 position,
            float targetX,
            float moveSpeed,
            PlatformGimmickSetting gimmickSetting)
        {
            var platform = _platformFactory.Get();
            if (platform == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Failed to get platform from factory.");
                return null;
            }

            var gimmickType = gimmickSetting == null ? PlatformGimmickType.Normal : gimmickSetting.Type;
            var gimmickBehaviour = _platformGimmickBehaviourFactory.Get(gimmickType);
            platform.Initialize(
                _nextPlatformIndex,
                gimmickType,
                position,
                targetX,
                _configData.PlatformLandingHeight,
                moveSpeed,
                gimmickBehaviour,
                gimmickSetting);
            _platformRegistry.Register(platform);
            _nextPlatformIndex++;
            return platform;
        }

        private PlatformGimmickSetting ResolveDoubleFollowUpGimmickSetting()
        {
            var settings = _configData.PlatformGimmickSettings;
            var normalSetting = FindPlatformGimmickSetting(settings, PlatformGimmickType.Normal);
            var smallSetting = FindPlatformGimmickSetting(settings, PlatformGimmickType.Small);
            var fastSetting = FindPlatformGimmickSetting(settings, PlatformGimmickType.Fast);

            if (smallSetting == null && fastSetting == null)
            {
                return normalSetting;
            }

            return UnityEngine.Random.value < 0.5f
                ? smallSetting ?? fastSetting
                : fastSetting ?? smallSetting;
        }

        private void ClearPendingDoubleSpawn()
        {
            _hasPendingDoublePreSpawn = false;
            _pendingDoublePreSpawnElapsed = 0f;
            _pendingDoublePreSpawnDelay = 0f;
            _pendingDoublePlatformY = 0f;
            _doubleSourcePlatform = null;
            _prefetchedDoublePlatform = null;
        }

        private void EnablePrefetchedDoublePlatform()
        {
            _prefetchedDoublePlatform?.SetInteractionEnabled(true);
            _prefetchedDoublePlatform = null;
        }

        private float ResolveBaseMoveSpeed()
        {
            var scoreFactor = Mathf.Clamp01(_scoreService.Score / Mathf.Max(1f, _configData.PlatformScoreSpeedMaxScore));
            var baseMoveSpeed = Mathf.Max(
                0f,
                _configData.PlatformBaseMoveSpeed + scoreFactor * _configData.PlatformMaxMoveSpeedBonus);
            var minScale = Mathf.Max(0f, _configData.PlatformBaseMoveSpeedMinScale);
            var maxScale = Mathf.Max(0f, _configData.PlatformBaseMoveSpeedMaxScale);
            if (maxScale < minScale)
            {
                maxScale = minScale;
            }

            return baseMoveSpeed * UnityEngine.Random.Range(minScale, maxScale);
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

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            if (ev.Platform == null)
            {
                return;
            }

            _platformRegistry.ReleaseBelow(ev.Platform.CenterY - Mathf.Max(0f, _configData.PlatformCleanupBelowDistance));
            if (ev.Platform == _doubleSourcePlatform)
            {
                SpawnPendingDoublePlatform();
                EnablePrefetchedDoublePlatform();
                _doubleSourcePlatform = null;
                return;
            }

            SpawnIncomingPlatformAtY(ev.Platform.GetStackedNextCenterY());
        }

        private float ResolveSpawnSide()
        {
            var firstDirection = _configData.PlatformFirstSpawnDirection >= 0 ? 1f : -1f;
            if (!_configData.PlatformAlternatesSpawnSide)
            {
                return firstDirection;
            }

            if (_configData.PlatformRandomSpawnSideStartCount > 0 &&
                _nextPlatformIndex + 1 >= _configData.PlatformRandomSpawnSideStartCount)
            {
                return UnityEngine.Random.value < 0.5f ? firstDirection : -firstDirection;
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
