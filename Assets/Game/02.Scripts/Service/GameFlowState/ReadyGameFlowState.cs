using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Presenter;

namespace JumJump.Service.GameFlowState
{
    public sealed class ReadyGameFlowState : IGameFlowState, IDisposable
    {
        public GameStateType StateType => GameStateType.Ready;

        private readonly IEventBus _eventBus;
        private readonly UIService _uiService;
        private readonly UIGameScenePresenter _scenePresenter;
        private readonly GameConfigData _configData;

        public ReadyGameFlowState(
            IEventBus eventBus,
            UIService uiService,
            UIGameScenePresenter scenePresenter,
            GameConfigData configData)
        {
            _eventBus = eventBus;
            _uiService = uiService;
            _scenePresenter = scenePresenter;
            _configData = configData;
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnResourcesReady);
        }

        public void OnEnter() { }
        public void OnUpdate() { }
        public void OnExit() { }

        public void OnTapRequested(IGameFlowStateContext context)
        {
            context.ChangeState(GameStateType.Playing);
        }

        public void OnPlayerMissedLanding(IGameFlowStateContext context)
        {
            context.ChangeState(GameStateType.GameOver);
        }

        public void OnRestartRequested(IGameFlowStateContext context)
        {
            context.ChangeState(GameStateType.Ready);
            _eventBus.Publish(new GameResetEvent());
        }

        private void OnResourcesReady(in GameResourcesReadyEvent ev)
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            var view = _uiService.Create<UI_GameScene>(_configData.GameSceneUiAddressableKey);
            _scenePresenter.Bind(view);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
        }
    }
}
