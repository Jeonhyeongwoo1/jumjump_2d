using System;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
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
        private readonly PlatformConfigData _platformConfigData;
        private readonly GameConfigData _gameConfigData;
        private readonly ScoreService _scoreService;
        private readonly PlayerRegistry _playerRegistry;
        private readonly Transform _poolRoot;

        private ScoreBoard _scoreBoardPrefab;
        private ScoreBoard _activeScoreBoard;
        private float _activeBaseY;
        private float _activeStepY;
        private bool _isReady;

        public ScoreBoardService(
            IEventBus eventBus,
            PoolService poolService,
            ResourceService resourceService,
            ResourceConfigData resourceConfigData,
            BackgroundConfigData backgroundConfigData,
            PlatformConfigData platformConfigData,
            GameConfigData gameConfigData,
            ScoreService scoreService,
            PlayerRegistry playerRegistry,
            Transform poolRoot)
        {
            _eventBus = eventBus;
            _poolService = poolService;
            _resourceService = resourceService;
            _resourceConfigData = resourceConfigData;
            _backgroundConfigData = backgroundConfigData;
            _platformConfigData = platformConfigData;
            _gameConfigData = gameConfigData;
            _scoreService = scoreService;
            _playerRegistry = playerRegistry;
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

            _eventBus.Subscribe<GameResourcesReadyEvent>(OnGameResourcesReady);
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            _isReady = true;
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnGameResourcesReady);
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
            ReleaseActive();
        }

        private ScoreBoard Create()
        {
            return UnityEngine.Object.Instantiate(_scoreBoardPrefab, _poolRoot);
        }

        private void OnGameResourcesReady(in GameResourcesReadyEvent ev)
        {
            TryActivateForRound();
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            if (!_isReady || _activeScoreBoard == null)
            {
                return;
            }

            UpdateActivePosition();
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            ReleaseActive();
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            ReleaseActive();
            TryActivateForRound();
        }

        private void TryActivateForRound()
        {
            if (!_isReady || _activeScoreBoard != null)
            {
                return;
            }

            var targetScore = _scoreService.RoundHighScoreTarget;
            if (targetScore <= GameConst.Score.ScoreBoardMinimumHighScore)
            {
                return;
            }

            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            _activeStepY = ResolvePlatformStepY();
            _activeBaseY = ResolveTargetBaseY(player.Position.y, targetScore, _activeStepY);

            var spriteIndex = ResolveSpriteIndex(_activeBaseY);
            var sprite = ResolveSprite(spriteIndex);
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
            view.Show(
                ResolveBoardPosition(player.Position.x, ResolveBoardY()),
                sprite,
                GameConst.Score.ScoreBoardSortingOrder,
                spriteIndex,
                targetScore);
        }

        private void ReleaseActive()
        {
            if (_activeScoreBoard == null)
            {
                ResetActiveState();
                return;
            }

            _poolService.Release(_resourceConfigData.ScoreBoardPoolKey, _activeScoreBoard);
            _activeScoreBoard = null;
            ResetActiveState();
        }

        private void ResetActiveState()
        {
            _activeBaseY = 0f;
            _activeStepY = 0f;
        }

        private void UpdateActivePosition()
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            _activeScoreBoard.transform.position = ResolveBoardPosition(player.Position.x, ResolveBoardY());
        }

        private Vector3 ResolveBoardPosition(float x, float y) => new Vector3(x, y, 0f);

        private float ResolveBoardY()
        {
            var scorePerLanding = Mathf.Max(1, _gameConfigData.ScorePerLanding);
            var scoreAheadOfPhysicalHeight = Mathf.Max(0, _scoreService.Score - _scoreService.BaseScore);
            var extraSteps = scoreAheadOfPhysicalHeight / (float)scorePerLanding;
            return _activeBaseY - extraSteps * _activeStepY;
        }

        private float ResolveTargetBaseY(float originY, int targetScore, float stepY)
        {
            var scorePerLanding = Mathf.Max(1, _gameConfigData.ScorePerLanding);
            var targetLandingCount = Mathf.CeilToInt(targetScore / (float)scorePerLanding);
            var targetStepCount = Mathf.Max(0, targetLandingCount - 1);
            return originY + targetStepCount * stepY + GameConst.Score.ScoreBoardTargetOffsetY;
        }

        private float ResolvePlatformStepY()
        {
            return Mathf.Max(0.01f, _platformConfigData.PlatformHeight) +
                   Mathf.Max(0f, _platformConfigData.PlatformStackVerticalOffset);
        }

        private Sprite ResolveSprite(int index)
        {
            var keys = _resourceConfigData.ScoreBoardSpriteAddressableKeys;
            if (keys == null || keys.Length == 0)
            {
                return null;
            }

            return _resourceService.GetAsset<Sprite>(keys[Mathf.Clamp(index, 0, keys.Length - 1)]);
        }

        private int ResolveSpriteIndex(float height)
        {
            var keys = _resourceConfigData.ScoreBoardSpriteAddressableKeys;
            if (keys == null || keys.Length == 0)
            {
                return 0;
            }

            return Mathf.Clamp(ResolveDepthLayerIndex(height), 0, keys.Length - 1);
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
