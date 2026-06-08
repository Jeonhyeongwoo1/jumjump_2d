using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Service;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UIDynamicFontPresenter : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;
        private readonly PlayerRegistry _playerRegistry;
        private readonly GameConfigData _configData;
        private readonly UIDynamicFont _root;

        private int _previousScore;

        public UIDynamicFontPresenter(
            IEventBus eventBus,
            PoolService poolService,
            ResourceService resourceService,
            PlayerRegistry playerRegistry,
            GameConfigData configData,
            UIDynamicFont root)
        {
            _eventBus = eventBus;
            _poolService = poolService;
            _resourceService = resourceService;
            _playerRegistry = playerRegistry;
            _configData = configData;
            _root = root;
        }

        public void Warmup()
        {
            var prefab = _resourceService.GetPrefab(_configData.DynamicFontAddressableKey);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(UIDynamicFontPresenter)}] Failed to load prefab: {_configData.DynamicFontAddressableKey}");
                return;
            }

            var fontPrefab = prefab.GetComponent<UI_DynamicFont>();
            var rootTransform = _root.transform;
            _poolService.Register<UI_DynamicFont>(
                _configData.DynamicFontPoolKey,
                () => UnityEngine.Object.Instantiate(fontPrefab, rootTransform),
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                _configData.DynamicFontPrewarmCount);

            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            var delta = ev.Score - _previousScore;
            _previousScore = ev.Score;

            if (delta <= 0)
            {
                return;
            }

            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            var spawnPosition = player.Position +
                                new Vector3(GameConst.DynamicFont.SpawnOffsetX, GameConst.DynamicFont.SpawnOffsetY, 0f);

            var view = _poolService.Get<UI_DynamicFont>(_configData.DynamicFontPoolKey);
            if (view == null)
            {
                return;
            }

            view.Show($"+{delta}", spawnPosition, Release);
        }

        private void Release(UI_DynamicFont view)
        {
            _poolService.Release(_configData.DynamicFontPoolKey, view);
        }
    }
}
