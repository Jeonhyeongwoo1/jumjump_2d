using JumJump.Event;
using JumJump.Interface;
using JumJump.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace JumJump.Presenter
{
    public sealed class GameHudPresenter : MonoBehaviour
    {
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _highScoreText;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _finalScoreText;
        [SerializeField] private Button _restartButton;

        private IEventBus _eventBus;
        private ScoreService _scoreService;

        [Inject]
        public void Construct(IEventBus eventBus, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
        }

        private void RefreshScore(int score, int highScore)
        {
            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }

            if (_highScoreText != null)
            {
                _highScoreText.text = highScore.ToString();
            }
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            RefreshScore(ev.Score, ev.HighScore);
        }

        private void OnGameStateChanged(in GameStateChangedEvent ev)
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(ev.State == GameStateType.GameOver);
            }
        }

        private void OnGameOver(in GameOverEvent ev)
        {
            if (_finalScoreText != null)
            {
                _finalScoreText.text = ev.Score.ToString();
            }
        }

        private void OnRestartClicked()
        {
            _eventBus.Publish(new RestartRequestedEvent());
        }

        private void Start()
        {
            if (_eventBus == null)
            {
                Debug.LogError($"[{nameof(GameHudPresenter)}] Missing dependency: {nameof(_eventBus)}.");
                enabled = false;
                return;
            }

            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
            _restartButton?.onClick.AddListener(OnRestartClicked);
            RefreshScore(_scoreService.Score, _scoreService.HighScore);

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            _eventBus?.Unsubscribe<GameOverEvent>(OnGameOver);
            _restartButton?.onClick.RemoveListener(OnRestartClicked);
        }
    }
}
