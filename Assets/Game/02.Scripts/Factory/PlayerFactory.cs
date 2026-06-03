using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Controller;
using JumJump.Registry;
using JumJump.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Factory
{
    public sealed class PlayerFactory
    {
        public const string AddressableKey = "Player";

        private const string PoolKey = "Player";
        private const int PrewarmCount = 1;

        private readonly PoolService _poolService;
        private readonly ResourceService _resourceService;
        private readonly PlayerRegistry _playerRegistry;
        private readonly IObjectResolver _resolver;

        private GameObject _playerPrefab;
        private bool _isReady;

        public PlayerFactory(
            PoolService poolService,
            ResourceService resourceService,
            PlayerRegistry playerRegistry,
            IObjectResolver resolver)
        {
            _poolService = poolService;
            _resourceService = resourceService;
            _playerRegistry = playerRegistry;
            _resolver = resolver;
        }

        public async UniTask WarmupAsync(CancellationToken cancellationToken = default)
        {
            if (_isReady)
            {
                return;
            }

            var prefab = await _resourceService.LoadPrefabAsync(AddressableKey, cancellationToken);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(PlayerFactory)}] Failed to load player prefab: {AddressableKey}");
                return;
            }

            if (prefab.GetComponent<PlayerJumpController>() == null)
            {
                Debug.LogError($"[{nameof(PlayerFactory)}] Loaded prefab has no {nameof(PlayerJumpController)}.");
                return;
            }

            _playerPrefab = prefab;
            _poolService.Register(PoolKey, Create, OnGet, OnRelease, PrewarmCount);
            _isReady = true;
        }

        public PlayerJumpController Spawn()
        {
            if (!_isReady)
            {
                Debug.LogError($"[{nameof(PlayerFactory)}] Spawn called before warmup.");
                return null;
            }

            var player = _poolService.Get<PlayerJumpController>(PoolKey);
            _playerRegistry.Set(player);
            return player;
        }

        private PlayerJumpController Create()
        {
            var instance = Object.Instantiate(_playerPrefab);
            _resolver.InjectGameObject(instance);
            return instance.GetComponent<PlayerJumpController>();
        }

        private void OnGet(PlayerJumpController player)
        {
            player.gameObject.SetActive(true);
        }

        private void OnRelease(PlayerJumpController player)
        {
            player.gameObject.SetActive(false);
        }
    }
}
