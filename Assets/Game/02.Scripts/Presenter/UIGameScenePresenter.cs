using JumJump.Event;
using JumJump.Interface;
using JumJump.Service;

namespace JumJump.Presenter
{
    public sealed class UIGameScenePresenter
    {
        private readonly IEventBus _eventBus;
        private readonly ScoreService _scoreService;
        private UI_GameScene _view;

        public UIGameScenePresenter(IEventBus eventBus, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
        }

        public void Bind(UI_GameScene view)
        {
            _view = view;
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _view.SetScore(_scoreService.Score);
            _view.SetGold(0);
        }

        public void Unbind()
        {
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _view = null;
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            _view?.SetScore(ev.Score);
        }
    }
}
