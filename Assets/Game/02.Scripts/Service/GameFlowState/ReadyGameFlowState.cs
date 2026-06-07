using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Presenter;

namespace JumJump.Service.GameFlowState
{
    public sealed class ReadyGameFlowState : IGameFlowState
    {
        public GameStateType StateType => GameStateType.Ready;

        private readonly IEventBus _eventBus;
        private readonly UIService _uiService;
        private readonly GameConfigData _configData;

        public ReadyGameFlowState(IEventBus eventBus, UIService uiService, GameConfigData configData)
        {
            _eventBus = eventBus;
            _uiService = uiService;
            _configData = configData;
            // GameBootstrapService가 비동기로 리소스를 로드한 뒤 이 이벤트를 발행한다.
            // OnEnter는 리소스 로드 전에 호출되므로 이벤트 기반으로 UI를 생성한다.
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnResourcesReady);
        }

        public void OnEnter()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnExit()
        {
        }

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
            _uiService.Create<UI_GameScene>(_configData.GameSceneUiAddressableKey);
        }
    }
}
