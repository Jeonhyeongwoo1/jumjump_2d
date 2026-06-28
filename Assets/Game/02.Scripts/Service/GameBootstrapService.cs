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
using JumJump.Util;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class GameBootstrapService : IInitializable, IAsyncStartable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly LoadingScreenService _loadingScreenService;
        private readonly ResourceService _resourceService;
        private readonly PlatformFactory _platformFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly UIDynamicFontPresenter _dynamicFontPresenter;
        private readonly ScoreBoardService _scoreBoardService;
        private readonly JumpBoxBoostFXService _jumpBoxBoostFXService;
        private readonly CoinCollectFXService _coinCollectFXService;
        private readonly PlayerDataRegistry _playerDataRegistry;
        private readonly ResourceConfigData _resourceConfigData;
        private readonly GameCheatConfigData _gameCheatConfigData;
        private readonly AuthRegistry _authRegistry;
        private bool _hasAuthFailed;

        [Inject]
        public GameBootstrapService(
            IEventBus eventBus,
            LoadingScreenService loadingScreenService,
            ResourceService resourceService,
            PlatformFactory platformFactory,
            PlayerFactory playerFactory,
            UIDynamicFontPresenter dynamicFontPresenter,
            ScoreBoardService scoreBoardService,
            JumpBoxBoostFXService jumpBoxBoostFXService,
            CoinCollectFXService coinCollectFXService,
            PlayerDataRegistry playerDataRegistry,
            ResourceConfigData resourceConfigData,
            GameCheatConfigData gameCheatConfigData,
            AuthRegistry authRegistry)
        {
            _eventBus = eventBus;
            _loadingScreenService = loadingScreenService;
            _resourceService = resourceService;
            _platformFactory = platformFactory;
            _playerFactory = playerFactory;
            _dynamicFontPresenter = dynamicFontPresenter;
            _scoreBoardService = scoreBoardService;
            _jumpBoxBoostFXService = jumpBoxBoostFXService;
            _coinCollectFXService = coinCollectFXService;
            _playerDataRegistry = playerDataRegistry;
            _resourceConfigData = resourceConfigData;
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
            var bootstrapStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Bootstrap");
            _loadingScreenService.Show();
            _loadingScreenService.SetProgress(0f);

            var workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Wait for auth");
            var authReady = await WaitForAuthAsync(cancellation);
            GameLogger.EndWork(nameof(GameBootstrapService), "Wait for auth", workStartedAt);
            if (!authReady)
            {
                GameLogger.Error(nameof(GameBootstrapService), "Auth failed; bootstrap aborted.");
                _loadingScreenService.Hide();
                GameLogger.EndWork(nameof(GameBootstrapService), "Bootstrap", bootstrapStartedAt);
                return;
            }

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Preload resources");
            await _resourceService.PreLoadAsync(_loadingScreenService.SetProgress, cancellation);
            GameLogger.EndWork(nameof(GameBootstrapService), "Preload resources", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Warmup platform factory");
            _platformFactory.Warmup();
            GameLogger.EndWork(nameof(GameBootstrapService), "Warmup platform factory", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Warmup player factory");
            _playerFactory.Warmup();
            GameLogger.EndWork(nameof(GameBootstrapService), "Warmup player factory", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Warmup dynamic font");
            _dynamicFontPresenter.Warmup();
            GameLogger.EndWork(nameof(GameBootstrapService), "Warmup dynamic font", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Warmup score board");
            _scoreBoardService.Warmup();
            GameLogger.EndWork(nameof(GameBootstrapService), "Warmup score board", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Warmup jump box boost FX");
            await _jumpBoxBoostFXService.WarmupAsync(cancellation);
            GameLogger.EndWork(nameof(GameBootstrapService), "Warmup jump box boost FX", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Warmup coin collect FX");
            await _coinCollectFXService.WarmupAsync(cancellation);
            GameLogger.EndWork(nameof(GameBootstrapService), "Warmup coin collect FX", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Spawn player");
            var player = _playerFactory.Spawn();
            if (player == null)
            {
                GameLogger.EndWork(nameof(GameBootstrapService), "Spawn player", workStartedAt);
                GameLogger.Error(nameof(GameBootstrapService), "Player spawn failed; aborting bootstrap.");
                _loadingScreenService.Hide();
                GameLogger.EndWork(nameof(GameBootstrapService), "Bootstrap", bootstrapStartedAt);
                return;
            }
            GameLogger.EndWork(nameof(GameBootstrapService), "Spawn player", workStartedAt);

            workStartedAt = GameLogger.BeginWork(nameof(GameBootstrapService), "Apply player skin");
            await ApplyPlayerSkinAsync(player, cancellation);
            GameLogger.EndWork(nameof(GameBootstrapService), "Apply player skin", workStartedAt);

            _eventBus.Publish(new PlayerSpawnedEvent(player));
            _eventBus.Publish(new GameResourcesReadyEvent());
            _loadingScreenService.SetProgress(1f);
            _loadingScreenService.Hide();
            GameLogger.EndWork(nameof(GameBootstrapService), "Bootstrap", bootstrapStartedAt);
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

        private async UniTask ApplyPlayerSkinAsync(Player player, CancellationToken cancellationToken)
        {
            if (_gameCheatConfigData.ForcePlayerSkin)
            {
                await ApplyForcedPlayerSkinAsync(player, cancellationToken);
                return;
            }

            var skinId = _playerDataRegistry.SelectedPlayerSkinId;
            await ApplyPlayerSkinAsync(player, skinId, cancellationToken);
        }

        private async UniTask ApplyForcedPlayerSkinAsync(Player player, CancellationToken cancellationToken)
        {
            var skinId = (int)_gameCheatConfigData.ForcedPlayerSkinType;
            await ApplyPlayerSkinAsync(player, skinId, cancellationToken);
        }

        private async UniTask ApplyPlayerSkinAsync(Player player, int skinId, CancellationToken cancellationToken)
        {
            if (skinId == (int)PlayerSkinType.Player_1)
            {
                player.ApplyDefaultSkin();
                return;
            }

            var key = ResolvePlayerSkinDataAddressableKey(skinId);
            var loaded = await _resourceService.LoadKeyAsync(key, cancellationToken);
            if (!loaded)
            {
                GameLogger.Error(nameof(GameBootstrapService), $"Failed to load player skin data: {key}");
                player.ApplyDefaultSkin();
                return;
            }

            var skinData = _resourceService.GetAsset<PlayerSkinData>(key);
            if (skinData == null || !player.TryApplySkin(skinData))
            {
                GameLogger.Error(nameof(GameBootstrapService), $"Failed to apply selected player skin: {skinId}");
                player.ApplyDefaultSkin();
            }
        }

        private string ResolvePlayerSkinDataAddressableKey(int skinId)
        {
            return _resourceConfigData.PlayerSkinDataAddressableKeyPrefix + skinId;
        }
    }
}
