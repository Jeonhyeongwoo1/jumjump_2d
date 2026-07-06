using JumJump.Controller;
using JumJump.Data;
using JumJump.Registry;
using JumJump.Service;
using JumJump.Util;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Factory
{
    public sealed class PlayerFactory
    {
        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;
        private readonly PlayerRegistry _playerRegistry;
        private readonly IObjectResolver _resolver;
        private readonly ResourceConfigData _configData;

        private GameObject _playerPrefab;
        private bool _isReady;

        [Inject]
        public PlayerFactory(
            PoolService poolService,
            ResourceService resourceService,
            PlayerRegistry playerRegistry,
            IObjectResolver resolver,
            ResourceConfigData configData)
        {
            _poolService = poolService;
            _resourceService = resourceService;
            _playerRegistry = playerRegistry;
            _resolver = resolver;
            _configData = configData;
        }

        public void Warmup()
        {
            if (_isReady)
            {
                return;
            }

            var prefab = _resourceService.GetPrefab(_configData.PlayerAddressableKey);
            if (prefab == null)
            {
                GameLogger.Error(nameof(PlayerFactory), $"Failed to load player prefab: {_configData.PlayerAddressableKey}");
                return;
            }

            _playerPrefab = prefab;
            _poolService.Register(_configData.PlayerPoolKey, Create, OnGet, OnRelease, _configData.PlayerPrewarmCount);
            _isReady = true;
        }

        public Player Spawn()
        {
            if (!_isReady)
            {
                GameLogger.Error(nameof(PlayerFactory), "Spawn called before warmup.");
                return null;
            }

            var player = _poolService.Get<Player>(_configData.PlayerPoolKey);
            _playerRegistry.Set(player);
            return player;
        }

        private Player Create()
        {
            var instance = Object.Instantiate(_playerPrefab);
            _resolver.InjectGameObject(instance);
            return instance.GetComponent<Player>();
        }

        private void OnGet(Player player)
        {
            player.gameObject.SetActive(true);
        }

        private void OnRelease(Player player)
        {
            player.gameObject.SetActive(false);
        }
    }
}
