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
        private float _pendingDoubleActivationElapsed;
        private float _pendingDoubleActivationDelay;
        private float _pendingDoublePlatformY;
        private bool _hasPendingDoublePreSpawn;
        private bool _hasPendingDoubleActivation;
        private PlatformController _doubleSourcePlatform;
        private PlatformController _prefetchedDoublePlatform;
        private readonly IEventBus _eventBus;
        private readonly PlatformFactory _platformFactory;
        private readonly PlatformRegistry _platformRegistry;
        private readonly PlayerRegistry _playerRegistry;
        private readonly ScoreService _scoreService;
        private readonly PlatformGimmickBehaviourFactory _platformGimmickBehaviourFactory;
        private readonly PlatformGimmickSelector _gimmickSelector;
        private readonly PlatformSpawnPositionResolver _positionResolver;
        private readonly GameConfigData _configData;

        public PlatformSpawnService(
            IEventBus eventBus,
            PlatformFactory platformFactory,
            PlatformRegistry platformRegistry,
            PlayerRegistry playerRegistry,
            ScoreService scoreService,
            PlatformGimmickBehaviourFactory platformGimmickBehaviourFactory,
            PlatformGimmickSelector gimmickSelector,
            PlatformSpawnPositionResolver positionResolver,
            GameConfigData configData)
        {
            _eventBus = eventBus;
            _platformFactory = platformFactory;
            _platformRegistry = platformRegistry;
            _playerRegistry = playerRegistry;
            _scoreService = scoreService;
            _platformGimmickBehaviourFactory = platformGimmickBehaviourFactory;
            _gimmickSelector = gimmickSelector;
            _positionResolver = positionResolver;
            _configData = configData;
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
            TickPendingDoublePreSpawn();
            TickPendingDoubleActivation();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        private void TickPendingDoublePreSpawn()
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

        private void TickPendingDoubleActivation()
        {
            if (!_hasPendingDoubleActivation)
            {
                return;
            }

            _pendingDoubleActivationElapsed += Time.deltaTime;
            if (_pendingDoubleActivationElapsed < _pendingDoubleActivationDelay)
            {
                return;
            }

            ActivatePendingDoublePlatform();
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
            var spawnSide = _positionResolver.ResolveSpawnSide(_nextPlatformIndex);
            var spawnX = _positionResolver.ResolveIncomingSpawnX(targetX, spawnSide);
            var gimmickSetting = _gimmickSelector.ResolvePrimary();
            var moveSpeed = ResolveMoveSpeed(gimmickSetting);
            if (gimmickSetting != null && gimmickSetting.Type == PlatformGimmickType.Double)
            {
                var normalSetting = _gimmickSelector.Find(PlatformGimmickType.Normal);
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
            var spawnSide = _positionResolver.ResolveSpawnSide(_nextPlatformIndex);
            var spawnX = _positionResolver.ResolveDoublePreviewSpawnX(targetX, spawnSide);
            var moveSpeed = ResolveBaseMoveSpeed() *
                            Mathf.Max(0f, _configData.PlatformDoubleFollowUpMoveSpeedScale);
            var gimmickSetting = _gimmickSelector.ResolveDoubleFollowUp();
            _prefetchedDoublePlatform = SpawnPlatform(
                new Vector3(spawnX, _pendingDoublePlatformY, 0f),
                targetX,
                moveSpeed,
                gimmickSetting);
            _prefetchedDoublePlatform?.EnterPreview(_configData.PlatformDoublePreviewAlpha);
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
                position,
                targetX,
                moveSpeed,
                gimmickBehaviour,
                gimmickSetting);
            _platformRegistry.Register(platform);
            _nextPlatformIndex++;
            return platform;
        }

        private float ResolveMoveSpeed(PlatformGimmickSetting gimmickSetting)
        {
            if (gimmickSetting != null && gimmickSetting.Type == PlatformGimmickType.Reveal)
            {
                return Mathf.Max(0f, _configData.PlatformBaseMoveSpeed);
            }

            return ResolveBaseMoveSpeed();
        }

        private void SpawnRocketDestinationPlatform(PlatformController sourcePlatform)
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                Debug.LogError($"[{nameof(PlatformSpawnService)}] Player not ready; cannot spawn rocket destination platform.");
                return;
            }

            var normalSetting = _gimmickSelector.Find(PlatformGimmickType.Normal);
            var targetY = ResolveRocketDestinationCenterY(sourcePlatform);
            var targetX = player.Position.x;
            var destinationPlatform = SpawnPlatform(
                new Vector3(targetX, targetY, 0f),
                targetX,
                0f,
                normalSetting);
            if (destinationPlatform == null)
            {
                return;
            }

            player.StartRocketBoost(destinationPlatform, destinationPlatform.GetLandingPosition(player));
        }

        private float ResolveRocketDestinationCenterY(PlatformController sourcePlatform)
        {
            var targetY = sourcePlatform.GetStackedNextCenterY();
            var stepHeight = Mathf.Max(0f, _configData.PlatformHeight + _configData.PlatformStackVerticalOffset);
            var extraStackCount = Mathf.Max(0, _configData.PlatformRocketBoostExtraStackCount);
            return targetY + stepHeight * extraStackCount;
        }

        private void ClearPendingDoubleSpawn()
        {
            _hasPendingDoublePreSpawn = false;
            _pendingDoublePreSpawnElapsed = 0f;
            _pendingDoublePreSpawnDelay = 0f;
            _pendingDoubleActivationElapsed = 0f;
            _pendingDoubleActivationDelay = 0f;
            _pendingDoublePlatformY = 0f;
            _hasPendingDoubleActivation = false;
            _doubleSourcePlatform = null;
            _prefetchedDoublePlatform = null;
        }

        private void SchedulePrefetchedDoubleActivation()
        {
            if (_prefetchedDoublePlatform == null)
            {
                return;
            }

            _pendingDoubleActivationDelay = Mathf.Max(0f, _configData.PlatformDoubleActivationDelay);
            _pendingDoubleActivationElapsed = 0f;
            _hasPendingDoubleActivation = true;

            if (_pendingDoubleActivationDelay <= 0f)
            {
                ActivatePendingDoublePlatform();
            }
        }

        private void ActivatePendingDoublePlatform()
        {
            if (!_hasPendingDoubleActivation)
            {
                return;
            }

            _hasPendingDoubleActivation = false;
            _prefetchedDoublePlatform?.ActivateFromPreview();
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

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            if (ev.Platform == null)
            {
                return;
            }

            _platformRegistry.ReleaseBelow(ev.Platform.CenterY - Mathf.Max(0f, _configData.PlatformCleanupBelowDistance));
            if (ev.Platform.GimmickType == PlatformGimmickType.Rocket)
            {
                ClearPendingDoubleSpawn();
                SpawnRocketDestinationPlatform(ev.Platform);
                return;
            }

            if (ev.Platform == _doubleSourcePlatform)
            {
                SpawnPendingDoublePlatform();
                SchedulePrefetchedDoubleActivation();
                _doubleSourcePlatform = null;
                return;
            }

            SpawnIncomingPlatformAtY(ev.Platform.GetStackedNextCenterY());
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
