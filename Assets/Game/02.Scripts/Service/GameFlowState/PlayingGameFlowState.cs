using JumJump.Event;
using JumJump.Interface;

namespace JumJump.Service.GameFlowState
{
    public sealed class PlayingGameFlowState : IGameFlowState
    {
        public GameStateType StateType => GameStateType.Playing;

        private readonly IEventBus _eventBus;

        public PlayingGameFlowState(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void OnEnter()
        {
            _eventBus.Publish(new GameStartedEvent());
        }

        public void OnUpdate()
        {
        }

        public void OnExit()
        {
        }

        public void OnTapRequested(IGameFlowStateContext context)
        {
            _eventBus.Publish(new PlayerJumpRequestedEvent());
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
