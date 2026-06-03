using JumJump.Controller;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Presenter;
using JumJump.Registry;
using JumJump.Service;
using VContainer;
using VContainer.Unity;

namespace JumJump
{
    public sealed class GameSceneLifeScope : LifetimeScope
    {
        [UnityEngine.SerializeField] private UnityEngine.InputSystem.InputActionAsset _inputActions;
        [UnityEngine.SerializeField] private PlayerJumpController _playerJumpController;
        [UnityEngine.SerializeField] private PlatformController _platformPrefab;
        [UnityEngine.SerializeField] private UnityEngine.Transform _platformPoolRoot;
        [UnityEngine.SerializeField] private GameHudPresenter _gameHudPresenter;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IEventBus, EventBus>(Lifetime.Scoped);
            builder.Register<PlatformRegistry>(Lifetime.Scoped);
            builder.RegisterInstance(_inputActions);
            builder.RegisterInstance(_platformPrefab);
            builder.RegisterInstance(_platformPoolRoot);
            builder.Register<PoolService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<PlatformFactory>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<InputActionTapService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<ScoreService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<GameFlowService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<PlatformSpawnService>(Lifetime.Scoped).AsSelf();

            builder.RegisterComponent(_playerJumpController);

            if (_gameHudPresenter != null)
            {
                builder.RegisterComponent(_gameHudPresenter);
            }
        }
    }
}
