using JumJump.Event;
using JumJump.Interface;

namespace JumJump.Service.GameFlowState
{
    public sealed class GameOverGameFlowState : IGameFlowState
    {
        public GameStateType StateType => GameStateType.GameOver;

        private readonly IEventBus _eventBus;
        private readonly ScoreService _scoreService;

        public GameOverGameFlowState(IEventBus eventBus, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
        }

        public void OnEnter()
        {
            _eventBus.Publish(new GameOverEvent(_scoreService.Score, _scoreService.HighScore));
        }

        public void OnUpdate()
        {
        }

        public void OnExit()
        {
        }

        public void OnTapRequested(IGameFlowStateContext context)
        {
            _eventBus.Publish(new RestartRequestedEvent());
        }

        public void OnPlayerMissedLanding(IGameFlowStateContext context)
        {
        }

        public void OnRestartRequested(IGameFlowStateContext context)
        {
            context.ChangeState(GameStateType.Ready);
            _eventBus.Publish(new GameResetEvent());
        }
    }
}
