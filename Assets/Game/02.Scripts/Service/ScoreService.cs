using System;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class ScoreService : IInitializable, IDisposable
    {
        private const string HighScoreKey = "JumJump.HighScore";

        public int Score => _score;
        public int HighScore => _highScore;

        private int _score;
        private int _highScore;
        private readonly IEventBus _eventBus;

        public ScoreService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Initialize()
        {
            _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            PublishScoreChanged();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            _score++;

            if (_score > _highScore)
            {
                _highScore = _score;
                PlayerPrefs.SetInt(HighScoreKey, _highScore);
                PlayerPrefs.Save();
            }

            PublishScoreChanged();
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            _score = 0;
            PublishScoreChanged();
        }

        private void PublishScoreChanged()
        {
            _eventBus.Publish(new ScoreChangedEvent(_score, _highScore));
        }
    }
}
