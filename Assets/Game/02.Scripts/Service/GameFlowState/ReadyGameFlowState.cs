using JumJump.Event;
using JumJump.Interface;

namespace JumJump.Service.GameFlowState
{
    public sealed class ReadyGameFlowState : IGameFlowState
    {
        public GameStateType StateType => GameStateType.Ready;

        private readonly IEventBus _eventBus;

        public ReadyGameFlowState(IEventBus eventBus)
        {
            _eventBus = eventBus;
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
    }
}
