using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Controller;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    /// <summary>
    /// Addressables 리소스 로드 → 풀 워밍업 → 플레이어 스폰 순서를 보장하는 비동기 부트스트랩.
    /// 모든 준비가 끝난 뒤에만 <see cref="GameResourcesReadyEvent"/> 를 발행해
    /// 게임 시작(플랫폼 배치, 입력 처리)이 미로드 상태에서 동작하지 않도록 한다.
    /// </summary>
    public sealed class GameBootstrapService : IAsyncStartable
    {
        private readonly IEventBus _eventBus;
        private readonly ResourceService _resourceService;
        private readonly PlatformFactory _platformFactory;
        private readonly PlayerFactory _playerFactory;

        public GameBootstrapService(
            IEventBus eventBus,
            ResourceService resourceService,
            PlatformFactory platformFactory,
            PlayerFactory playerFactory)
        {
            _eventBus = eventBus;
            _resourceService = resourceService;
            _platformFactory = platformFactory;
            _playerFactory = playerFactory;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            await _resourceService.PreLoadAsync(cancellation);
            _platformFactory.Warmup();
            _playerFactory.Warmup();

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
