using System;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
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
        private float _pendingShieldBlockedRespawnElapsed;
        private bool _hasPendingDoublePreSpawn;
        private bool _hasPendingDoubleActivation;
        private bool _hasPendingShieldBlockedRespawn;
        private bool _hasPendingReviveSpawn;
        private PlatformController _doubleSourcePlatform;
        private PlatformController _prefetchedDoublePlatform;
        private PlatformController _lastLandedPlatform;
        private float _pendingRevivePlatformY;
        private readonly IEventBus _eventBus;
        private readonly PlatformFactory _platformFactory;
        private readonly PlatformRegistry _platformRegistry;
        private readonly PlayerRegistry _playerRegistry;
        private readonly ScoreService _scoreService;
        private readonly PlatformGimmickBehaviourFactory _platformGimmickBehaviourFactory;
        private readonly PlatformGimmickSelector _gimmickSelector;
        private readonly PlatformSpawnPositionResolver _positionResolver;
        private readonly PlatformConfigData _configData;
        private readonly GameConfigData _gameConfigData;
        private readonly PlayerConfigData _playerConfigData;

        public PlatformSpawnService(
            IEventBus eventBus,
            PlatformFactory platformFactory,
            PlatformRegistry platformRegistry,
            PlayerRegistry playerRegistry,
            ScoreService scoreService,
            PlatformGimmickBehaviourFactory platformGimmickBehaviourFactory,
            PlatformGimmickSelector gimmickSelector,
            PlatformSpawnPositionResolver positionResolver,
            PlatformConfigData configData,
            GameConfigData gameConfigData,
            PlayerConfigData playerConfigData)
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
            _gameConfigData = gameConfigData;
            _playerConfigData = playerConfigData;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Subscribe<PlatformShieldBlockedEvent>(OnPlatformShieldBlocked);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            _eventBus.Subscribe<GameRevivedEvent>(OnGameRevived);
        }

        public void Tick()
        {
            TickPendingDoublePreSpawn();
            TickPendingDoubleActivation();
            TickPendingShieldBlockedRespawn();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<PlatformShieldBlockedEvent>(OnPlatformShieldBlocked);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
            _eventBus.Unsubscribe<GameRevivedEvent>(OnGameRevived);
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
                GameLogger.Error(nameof(PlatformSpawnService), "Player not ready; cannot reset round.");
                return;
            }

            _platformRegistry.Clear();
            _nextPlatformIndex = 0;
            _lastLandedPlatform = null;
            _hasPendingReviveSpawn = false;
            _pendingRevivePlatformY = 0f;
            ClearPendingDoubleSpawn();
            ClearPendingShieldBlockedRespawn();
            player.ResetForRound();
        }

        private void TickPendingShieldBlockedRespawn()
        {
            if (!_hasPendingShieldBlockedRespawn)
            {
                return;
            }

            _pendingShieldBlockedRespawnElapsed += Time.deltaTime;
            if (_pendingShieldBlockedRespawnElapsed < GameConst.Platform.ShieldBlockedRespawnDelay)
            {
                return;
            }

            _hasPendingShieldBlockedRespawn = false;
            _pendingShieldBlockedRespawnElapsed = 0f;
            SpawnIncomingPlatform();
        }

        private PlatformController SpawnIncomingPlatform()
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                GameLogger.Error(nameof(PlatformSpawnService), "Player not ready; cannot spawn platform.");
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
                GameLogger.Error(nameof(PlatformSpawnService), "Player not ready; cannot spawn platform.");
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
                GameLogger.Error(nameof(PlatformSpawnService), "Player not ready; cannot spawn pending double platform.");
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
            var gimmickType = gimmickSetting == null ? PlatformGimmickType.Normal : gimmickSetting.Type;
            var platform = _platformFactory.Get(gimmickType);
            if (platform == null)
            {
                GameLogger.Error(nameof(PlatformSpawnService), "Failed to get platform from factory.");
                return null;
            }

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

        private void SpawnRocketPathPlatforms(PlatformController sourcePlatform)
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                GameLogger.Error(nameof(PlatformSpawnService), "Player not ready; cannot spawn rocket path platforms.");
                return;
            }

            var normalSetting = _gimmickSelector.Find(PlatformGimmickType.Normal);
            var targetX = player.Position.x;
            var platformCount = Mathf.Max(1, _gameConfigData.RocketBoostPlatformCount);
            var platformY = sourcePlatform.GetStackedNextCenterY();
            var entryDuration = ResolveRocketPathEntryDuration(platformCount);
            var destinationPlatform = default(PlatformController);
            var passedPlatformCount = 0;
            for (var i = 0; i < platformCount; i++)
            {
                var spawnSide = _positionResolver.ResolveSpawnSide(_nextPlatformIndex);
                var spawnX = _positionResolver.ResolveDoublePreviewSpawnX(targetX, spawnSide);
                var moveSpeed = ResolveRocketPathMoveSpeed(spawnX, targetX, entryDuration);
                var platform = SpawnPlatform(
                    new Vector3(spawnX, platformY, 0f),
                    targetX,
                    moveSpeed,
                    normalSetting);
                if (platform == null)
                {
                    continue;
                }

                platformY = platform.GetStackedNextCenterY();
                platform.DelayMove(ResolveRocketPathEntryDelay(i, platformCount, entryDuration), true);
                if (i < platformCount - 1)
                {
                    passedPlatformCount++;
                    platform.SetInteractionEnabled(false);
                    continue;
                }

                destinationPlatform = platform;
            }

            if (destinationPlatform == null)
            {
                return;
            }

            var destinationPosition = destinationPlatform.GetLandingPosition(player);
            destinationPosition.x = targetX;
            _eventBus.Publish(new RocketBoostPlatformsPassedEvent(passedPlatformCount));
            player.StartRocketBoost(destinationPlatform, destinationPosition);
        }

        private float ResolveRocketPathEntryDuration(int platformCount)
        {
            var duration = Mathf.Max(0.01f, _playerConfigData.PlayerRocketBoostDuration);
            return duration / Mathf.Max(1, platformCount) *
                   Mathf.Max(0.01f, GameConst.Platform.RocketPathEntryDurationScale);
        }

        private float ResolveRocketPathEntryDelay(int platformIndex, int platformCount, float entryDuration)
        {
            var proposedDelay = entryDuration * (platformIndex + GameConst.Platform.RocketPathPlayerLeadEntryRatio);
            var totalDuration = entryDuration * Mathf.Max(1, platformCount);
            var latestArrivalTime = totalDuration -
                                    entryDuration * GameConst.Platform.RocketPathFinalArrivalLeadEntryRatio;
            var latestDelay = Mathf.Max(0f, latestArrivalTime - entryDuration);
            return Mathf.Min(proposedDelay, latestDelay);
        }

        private float ResolveRocketPathMoveSpeed(float spawnX, float targetX, float duration)
        {
            return Mathf.Abs(spawnX - targetX) / Mathf.Max(0.01f, duration);
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

        private void ClearPendingShieldBlockedRespawn()
        {
            _hasPendingShieldBlockedRespawn = false;
            _pendingShieldBlockedRespawnElapsed = 0f;
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
            var scoreFactor = Mathf.Clamp01(_scoreService.BaseScore / Mathf.Max(1f, _configData.PlatformScoreSpeedMaxScore));
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

            _lastLandedPlatform = ev.Platform;
            _platformRegistry.ArchiveBelow(ResolveCleanupBelowY(ev.Platform));
            if (ev.Platform.GimmickType == PlatformGimmickType.Rocket)
            {
                ClearPendingDoubleSpawn();
                SpawnRocketPathPlatforms(ev.Platform);
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

        private float ResolveCleanupBelowY(PlatformController landedPlatform)
        {
            var cleanupDistance = Mathf.Max(0f, _configData.PlatformCleanupBelowDistance);
            var platformStep = Mathf.Max(0f, landedPlatform.GetStackedNextCenterY() - landedPlatform.CenterY);
            return landedPlatform.CenterY - cleanupDistance - platformStep;
        }

        private void OnPlatformShieldBlocked(in PlatformShieldBlockedEvent ev)
        {
            if (ev.Platform == null)
            {
                return;
            }

            _platformRegistry.Unregister(ev.Platform);
            ClearPendingDoubleSpawn();
            _pendingShieldBlockedRespawnElapsed = 0f;
            _hasPendingShieldBlockedRespawn = true;
        }

        private void OnResourcesReady(in GameResourcesReadyEvent ev)
        {
            ResetRound();
        }

        private void OnGameStarted(in GameStartedEvent ev)
        {
            if (_hasPendingReviveSpawn)
            {
                _hasPendingReviveSpawn = false;
                SpawnIncomingPlatformAtY(_pendingRevivePlatformY);
                _pendingRevivePlatformY = 0f;
                return;
            }

            SpawnIncomingPlatform();
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            ResetRound();
        }

        private void OnGameRevived(in GameRevivedEvent ev)
        {
            TryPrepareReviveAtLastLandedPlatform();
        }

        private void TryPrepareReviveAtLastLandedPlatform()
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                GameLogger.Error(nameof(PlatformSpawnService), "Player not ready; cannot revive.");
                return;
            }

            if (_lastLandedPlatform == null)
            {
                GameLogger.Error(nameof(PlatformSpawnService), "Last landed platform missing; cannot revive.");
                ResetRound();
                return;
            }

            ClearPendingDoubleSpawn();
            ClearPendingShieldBlockedRespawn();
            _platformRegistry.ReleaseAbove(_lastLandedPlatform.CenterY);
            _platformRegistry.ArchiveBelow(ResolveCleanupBelowY(_lastLandedPlatform));
            var revivePosition = _lastLandedPlatform.GetLandingPosition(player);
            revivePosition.x = player.SpawnX;
            player.ReviveAt(revivePosition);
            _pendingRevivePlatformY = _lastLandedPlatform.GetStackedNextCenterY();
            _hasPendingReviveSpawn = true;
        }
    }
}
