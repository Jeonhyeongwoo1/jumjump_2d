using JumJump.Event;
using JumJump.Interface;
using JumJump.Presenter;

namespace JumJump.Service.GameFlowState
{
    public sealed class GameOverGameFlowState : IGameFlowState
    {
        public GameStateType StateType => GameStateType.GameOver;

        private readonly IEventBus _eventBus;
        private readonly ScoreService _scoreService;
        private readonly UIGameOverPopupPresenter _popupPresenter;

        public GameOverGameFlowState(
            IEventBus eventBus,
            ScoreService scoreService,
            UIGameOverPopupPresenter popupPresenter)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
            _popupPresenter = popupPresenter;
        }

        public void OnEnter()
        {
            _eventBus.Publish(new GameOverEvent(_scoreService.Score, _scoreService.HighScore));
            _popupPresenter.Show();
        }

        public void OnUpdate(IGameFlowStateContext context) { }

        public void OnExit()
        {
            _popupPresenter.Hide();
        }

        public void OnTapRequested(IGameFlowStateContext context)
        {
            if (_popupPresenter.IsShowing) return;
            _eventBus.Publish(new RestartClickedEvent());
            _eventBus.Publish(new RestartRequestedEvent());
        }

        public void OnPlayerMissedLanding(IGameFlowStateContext context) { }

        public void OnRestartRequested(IGameFlowStateContext context)
        {
            context.ChangeState(GameStateType.Ready);
            _eventBus.Publish(new GameResetEvent());
        }

        public void OnReviveRequested(IGameFlowStateContext context)
        {
            _eventBus.Publish(new GameRevivedEvent());
            context.ChangeState(GameStateType.Ready);
        }
    }
}
