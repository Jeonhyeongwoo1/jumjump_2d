using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Presenter;
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

        public GameBootstrapService(
            IEventBus eventBus,
            ResourceService resourceService,
            PlatformFactory platformFactory,
            PlayerFactory playerFactory,
            UIDynamicFontPresenter dynamicFontPresenter)
        {
            _eventBus = eventBus;
            _resourceService = resourceService;
            _platformFactory = platformFactory;
            _playerFactory = playerFactory;
            _dynamicFontPresenter = dynamicFontPresenter;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await _resourceService.PreLoadAsync(cancellation);
            _platformFactory.Warmup();
            _playerFactory.Warmup();
            _dynamicFontPresenter.Warmup();

            var player = _playerFactory.Spawn();
            if (player == null)
            {
                Debug.LogError($"[{nameof(GameBootstrapService)}] Player spawn failed; aborting bootstrap.");
                return;
            }

            _eventBus.Publish(new PlayerSpawnedEvent(player));
            _eventBus.Publish(new GameResourcesReadyEvent());
        }
    }
}
