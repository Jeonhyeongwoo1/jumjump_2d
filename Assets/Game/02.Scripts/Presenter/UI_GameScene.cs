using JumJump.Event;
using JumJump.Interface;
using JumJump.Service;
using TMPro;
using UnityEngine;
using VContainer;

namespace JumJump.Presenter
{
    public sealed class UI_GameScene : BaseSceneUI
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _goldText;

        private IEventBus _eventBus;
        private ScoreService _scoreService;

        [Inject]
        public void Construct(IEventBus eventBus, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
        }

        private void RefreshScore(int score)
        {
            if (_scoreText != null) _scoreText.text = score.ToString();
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            RefreshScore(ev.Score);
        }

        private void Start()
        {
            if (_eventBus == null || _scoreService == null)
            {
                Debug.LogError($"[{nameof(UI_GameScene)}] Missing dependencies.");
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
            RefreshScore(_scoreService.Score);

            if (_goldText != null) _goldText.text = "0";
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
        }
    }
}
