using JumJump.Camera;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Service;
using JumJump.Service.GameFlowState;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace JumJump
{
    public sealed class GameSceneLifeScope : LifetimeScope
    {
        [UnityEngine.SerializeField] private GameConfigData _gameConfigData;
        [UnityEngine.SerializeField] private PlatformCheatData _platformCheatData;
        [UnityEngine.SerializeField] private InputActionAsset _inputActions;
        [UnityEngine.SerializeField] private VerticalFollowCamera _followCamera;
        [UnityEngine.SerializeField] private UnityEngine.Transform _platformPoolRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_gameConfigData == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(GameSceneLifeScope)}] Missing {nameof(GameConfigData)}.");
            }

            builder.RegisterInstance(_gameConfigData);
            builder.RegisterInstance(_platformCheatData);
            builder.Register<IEventBus, EventBus>(Lifetime.Scoped);
            builder.Register<ResourceService>(Lifetime.Scoped);
            builder.Register<PoolService>(Lifetime.Scoped);
            builder.Register<PlatformRegistry>(Lifetime.Scoped);
            builder.Register<PlayerRegistry>(Lifetime.Scoped);
            builder.RegisterInstance(_inputActions);
            builder.RegisterInstance(_platformPoolRoot);
            builder.Register<BackgroundEnvironmentFactory>(Lifetime.Scoped);
            builder.Register<UIFactory>(Lifetime.Scoped);
            builder.Register<UIService>(Lifetime.Scoped);
            builder.Register<PlatformFactory>(Lifetime.Scoped);
            builder.Register<PlatformGimmickBehaviourFactory>(Lifetime.Scoped);
            builder.Register<PlayerFactory>(Lifetime.Scoped);
            builder.Register<ReadyGameFlowState>(Lifetime.Scoped);
            builder.Register<PlayingGameFlowState>(Lifetime.Scoped);
            builder.Register<GameOverGameFlowState>(Lifetime.Scoped);
            builder.RegisterEntryPoint<InputActionTapService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<ScoreService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<GameFlowService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<BackgroundEnvironmentService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<PlatformSpawnService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<GameBootstrapService>(Lifetime.Scoped).AsSelf();

            if (_followCamera != null)
            {
                builder.RegisterComponent(_followCamera);
            }
        }
    }
}
