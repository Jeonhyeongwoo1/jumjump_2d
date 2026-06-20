using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Controller;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Presenter;
using JumJump.Registry;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class GameBootstrapService : IAsyncStartable
    {
        private readonly IEventBus _eventBus;
        private readonly ResourceService _resourceService;
        private readonly PlatformFactory _platformFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly UIDynamicFontPresenter _dynamicFontPresenter;
        private readonly ScoreBoardService _scoreBoardService;
        private readonly JumpBoxBoostFXService _jumpBoxBoostFXService;
        private readonly CoinCollectFXService _coinCollectFXService;
        private readonly PlayerDataRegistry _playerDataRegistry;
        private readonly GameCheatConfigData _gameCheatConfigData;

        public GameBootstrapService(
            IEventBus eventBus,
            ResourceService resourceService,
            PlatformFactory platformFactory,
            PlayerFactory playerFactory,
            UIDynamicFontPresenter dynamicFontPresenter,
            ScoreBoardService scoreBoardService,
            JumpBoxBoostFXService jumpBoxBoostFXService,
            CoinCollectFXService coinCollectFXService,
            PlayerDataRegistry playerDataRegistry,
            GameCheatConfigData gameCheatConfigData)
        {
            _eventBus = eventBus;
            _resourceService = resourceService;
            _platformFactory = platformFactory;
            _playerFactory = playerFactory;
            _dynamicFontPresenter = dynamicFontPresenter;
            _scoreBoardService = scoreBoardService;
            _jumpBoxBoostFXService = jumpBoxBoostFXService;
            _coinCollectFXService = coinCollectFXService;
            _playerDataRegistry = playerDataRegistry;
            _gameCheatConfigData = gameCheatConfigData;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await _resourceService.PreLoadAsync(cancellation);
            _platformFactory.Warmup();
            _playerFactory.Warmup();
            _dynamicFontPresenter.Warmup();
            _scoreBoardService.Warmup();
            await _jumpBoxBoostFXService.WarmupAsync(cancellation);
            await _coinCollectFXService.WarmupAsync(cancellation);

            var player = _playerFactory.Spawn();
            if (player == null)
            {
                Debug.LogError($"[{nameof(GameBootstrapService)}] Player spawn failed; aborting bootstrap.");
                return;
            }

            ApplyPlayerSkin(player);

            _eventBus.Publish(new PlayerSpawnedEvent(player));
            _eventBus.Publish(new GameResourcesReadyEvent());
        }

        private void ApplyPlayerSkin(Player player)
        {
            if (_gameCheatConfigData.ForcePlayerSkin)
            {
                ApplyForcedPlayerSkin(player);
                return;
            }

            var skinId = _playerDataRegistry.SelectedPlayerSkinId;
            if (!player.TryApplySkin(skinId))
            {
                Debug.LogError($"[{nameof(GameBootstrapService)}] Failed to apply selected player skin: {skinId}");
            }
        }

        private void ApplyForcedPlayerSkin(Player player)
        {
            var skinId = (int)_gameCheatConfigData.ForcedPlayerSkinType;
            if (!player.TryApplySkin(skinId))
            {
                Debug.LogError($"[{nameof(GameBootstrapService)}] Failed to apply forced player skin: {_gameCheatConfigData.ForcedPlayerSkinType}");
                return;
            }

            _playerDataRegistry.SetSelectedPlayerSkinId(skinId);
        }
    }
}
