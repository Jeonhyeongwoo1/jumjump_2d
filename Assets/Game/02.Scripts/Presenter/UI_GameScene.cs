using JumJump.Event;
using JumJump.Interface;
using JumJump.Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace JumJump.Presenter
{
    public sealed class UI_GameScene : BaseSceneUI
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _highScoreText;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TMP_Text _finalScoreText;
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
            if (_scoreText != null) _scoreText.text = score.ToString();
            if (_highScoreText != null) _highScoreText.text = highScore.ToString();
        }

        private void ShowGameOverPanel(int finalScore)
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
            if (_finalScoreText != null) _finalScoreText.text = finalScore.ToString();
        }

        private void HideGameOverPanel()
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            RefreshScore(ev.Score, ev.HighScore);
        }

        private void OnGameStateChanged(in GameStateChangedEvent ev)
        {
            if (ev.State == GameStateType.GameOver)
                ShowGameOverPanel(_scoreService?.Score ?? 0);
            else
                HideGameOverPanel();
        }

        private void OnGameOver(in GameOverEvent ev)
        {
            if (_finalScoreText != null) _finalScoreText.text = ev.Score.ToString();
        }

        private void OnRestartClicked()
        {
            _eventBus?.Publish(new RestartRequestedEvent());
        }

        protected override void Awake()
        {
            base.Awake();
            HideGameOverPanel();
        }

        private void Start()
        {
            if (_eventBus == null || _scoreService == null)
            {
                Debug.LogError($"[{nameof(UI_GameScene)}] Missing dependencies: {nameof(_eventBus)} or {nameof(_scoreService)}.");
                enabled = false;
                return;
            }

            if (_scoreText == null)
            {
                Debug.LogError($"[{nameof(UI_GameScene)}] Missing UI reference: {nameof(_scoreText)}.");
                enabled = false;
                return;
            }

            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
            _restartButton?.onClick.AddListener(OnRestartClicked);
            RefreshScore(_scoreService.Score, _scoreService.HighScore);
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
