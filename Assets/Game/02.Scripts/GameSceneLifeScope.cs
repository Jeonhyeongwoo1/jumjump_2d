using JumJump.Camera;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Presenter;
using JumJump.Registry;
using JumJump.Service;
using JumJump.Service.GameFlowState;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace JumJump
{
    public sealed class GameSceneLifeScope : LifetimeScope
    {
        [SerializeField] private ResourceConfigData _resourceConfigData;
        [SerializeField] private GameConfigData _gameConfigData;
        [SerializeField] private PlayerConfigData _playerConfigData;
        [SerializeField] private PlatformConfigData _platformConfigData;
        [SerializeField] private BackgroundConfigData _backgroundConfigData;
        [SerializeField] private EffectConfigData _effectConfigData;
        [SerializeField] private PlatformCheatData _platformCheatData;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private VerticalFollowCamera _followCamera;
        [SerializeField] private UnityEngine.Camera _gameCamera;
        [SerializeField] private Transform _platformPoolRoot;
        [SerializeField] private UIDynamicFont _dynamicFontRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_dynamicFontRoot);
            builder.RegisterInstance(_resourceConfigData);
            builder.RegisterInstance(_gameConfigData);
            builder.RegisterInstance(_playerConfigData);
            builder.RegisterInstance(_platformConfigData);
            builder.RegisterInstance(_backgroundConfigData);
            builder.RegisterInstance(_effectConfigData);
            builder.RegisterInstance(_platformCheatData);
            builder.Register<IEventBus, EventBus>(Lifetime.Scoped);
            builder.Register<ResourceService>(Lifetime.Scoped);
            builder.Register<PoolService>(Lifetime.Scoped);
            builder.Register<PlatformRegistry>(Lifetime.Scoped);
            builder.Register<PlayerRegistry>(Lifetime.Scoped);
            builder.RegisterInstance(_inputActions);
            builder.RegisterInstance(_platformPoolRoot);
            builder.RegisterInstance(_gameCamera);
            builder.RegisterComponent(_followCamera);
            builder.Register<BackgroundEnvironmentFactory>(Lifetime.Scoped);
            builder.Register<UIFactory>(Lifetime.Scoped);
            builder.Register<UIService>(Lifetime.Scoped);
            builder.Register<PopupService>(Lifetime.Scoped);
            builder.Register<PlatformGimmickSelector>(Lifetime.Scoped);
            builder.Register<PlatformSpawnPositionResolver>(Lifetime.Scoped);
            builder.Register<UIGameScenePresenter>(Lifetime.Scoped);
            builder.Register<UIGameOverPopupPresenter>(Lifetime.Scoped);
            builder.Register<UIDynamicFontPresenter>(Lifetime.Scoped);
            builder.Register<ScoreBoardService>(Lifetime.Scoped);
            builder.Register<PlatformFactory>(Lifetime.Scoped);
            builder.Register<PlatformGimmickBehaviourFactory>(Lifetime.Scoped);
            builder.Register<PlayerFactory>(Lifetime.Scoped);
            builder.Register<JumpBoxBoostFXService>(Lifetime.Scoped);
            builder.Register<ReadyGameFlowState>(Lifetime.Scoped);
            builder.Register<PlayingGameFlowState>(Lifetime.Scoped);
            builder.Register<GameOverGameFlowState>(Lifetime.Scoped);
            builder.RegisterEntryPoint<InputActionTapService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<ScoreService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<GameFlowService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<HayLandingFXService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<BackgroundEnvironmentService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<PlatformSpawnService>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<GameBootstrapService>(Lifetime.Scoped).AsSelf();
        }
    }
}
