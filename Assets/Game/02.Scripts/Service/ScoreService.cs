using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Util;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class ScoreService : IInitializable, ITickable, IDisposable
    {
        public int Score => _score;
        public int HighScore => _highScore;

        private int _score;
        private int _highScore;
        private int _pendingAnimatedScore;
        private float _pendingAnimatedScoreElapsed;
        private readonly IEventBus _eventBus;
        private readonly GameConfigData _configData;

        public ScoreService(IEventBus eventBus, GameConfigData configData)
        {
            _eventBus = eventBus;
            _configData = configData;
        }

        public void Initialize()
        {
            _highScore = PlayerPrefs.GetInt(_configData.HighScoreKey, 0);
            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Subscribe<RocketBoostPlatformsPassedEvent>(OnRocketBoostPlatformsPassed);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            PublishScoreChanged();
        }

        public void Tick()
        {
            if (_pendingAnimatedScore <= 0)
            {
                return;
            }

            _pendingAnimatedScoreElapsed += Time.deltaTime;
            if (_pendingAnimatedScoreElapsed < GameConst.Score.RocketBoostIncrementInterval)
            {
                return;
            }

            _pendingAnimatedScoreElapsed = 0f;
            _pendingAnimatedScore--;
            AddScore(1);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<RocketBoostPlatformsPassedEvent>(OnRocketBoostPlatformsPassed);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            AddScore(Mathf.Max(0, _configData.ScorePerLanding));
        }

        private void OnRocketBoostPlatformsPassed(in RocketBoostPlatformsPassedEvent ev)
        {
            QueueAnimatedScore(Mathf.Max(0, ev.PlatformCount) * Mathf.Max(0, _configData.ScorePerLanding));
        }

        private void QueueAnimatedScore(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _pendingAnimatedScore += amount;
        }

        private void AddScore(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _score += amount;

            if (_score > _highScore)
            {
                _highScore = _score;
                PlayerPrefs.SetInt(_configData.HighScoreKey, _highScore);
                PlayerPrefs.Save();
            }

            PublishScoreChanged();
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            _pendingAnimatedScore = 0;
            _pendingAnimatedScoreElapsed = 0f;
            _score = 0;
            PublishScoreChanged();
        }

        private void PublishScoreChanged()
        {
            _eventBus.Publish(new ScoreChangedEvent(_score, _highScore));
        }
    }
}
