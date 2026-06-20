using System;
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
    public sealed class GameBootstrapService : IInitializable, IAsyncStartable, IDisposable
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
        private readonly AuthRegistry _authRegistry;
        private bool _hasAuthFailed;

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
            GameCheatConfigData gameCheatConfigData,
            AuthRegistry authRegistry)
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
            _authRegistry = authRegistry;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<AuthLoginCompletedEvent>(OnAuthLoginCompleted);
            _eventBus.Subscribe<AuthLoginFailedEvent>(OnAuthLoginFailed);
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            var authReady = await WaitForAuthAsync(cancellation);
            if (!authReady)
            {
                Debug.LogError($"[{nameof(GameBootstrapService)}] Auth failed; bootstrap aborted.");
                return;
            }

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

        public void Dispose()
        {
            _eventBus.Unsubscribe<AuthLoginCompletedEvent>(OnAuthLoginCompleted);
            _eventBus.Unsubscribe<AuthLoginFailedEvent>(OnAuthLoginFailed);
        }

        private async UniTask<bool> WaitForAuthAsync(CancellationToken cancellation)
        {
            if (_authRegistry.IsLoggedIn)
            {
                return true;
            }

            await UniTask.WaitUntil(
                () => _authRegistry.IsLoggedIn || _hasAuthFailed,
                cancellationToken: cancellation);

            return _authRegistry.IsLoggedIn;
        }

        private void OnAuthLoginCompleted(in AuthLoginCompletedEvent ev)
        {
            _hasAuthFailed = false;
        }

        private void OnAuthLoginFailed(in AuthLoginFailedEvent ev)
        {
            _hasAuthFailed = true;
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
