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
        public int BaseScore => _baseScore;
        public int HighScore => _highScore;
        public int ComboScore => _comboScore;

        private int _score;
        private int _baseScore;
        private int _highScore;
        private int _comboScore;
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
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
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
            AddBaseScore(1);
            AddScore(1, 1, 0, false);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<RocketBoostPlatformsPassedEvent>(OnRocketBoostPlatformsPassed);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            UpdateComboScore(ev);
            var baseScore = Mathf.Max(0, _configData.ScorePerLanding);
            var comboBonus = ResolveComboBonus();
            AddBaseScore(baseScore);
            AddScore(baseScore + comboBonus, baseScore, comboBonus, _comboScore > 0);
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            ResetComboScore();
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

        private void AddScore(int amount, int baseScoreDelta, int comboBonusDelta, bool isComboLandingScore)
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

            PublishScoreChanged(amount, baseScoreDelta, comboBonusDelta, isComboLandingScore);
        }

        private void AddBaseScore(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _baseScore += amount;
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            _pendingAnimatedScore = 0;
            _pendingAnimatedScoreElapsed = 0f;
            _score = 0;
            _baseScore = 0;
            _comboScore = 0;
            PublishScoreChanged(0, 0, 0, false);
        }

        private void UpdateComboScore(in PlayerLandedEvent ev)
        {
            if (ev.Platform == null)
            {
                ResetComboScore();
                return;
            }

            var tolerance = Mathf.Max(0f, _configData.ComboLandingCenterTolerance);
            if (Mathf.Abs(ev.Platform.CenterX - ev.LandingPosition.x) <= tolerance)
            {
                _comboScore++;
                return;
            }

            _comboScore = 0;
        }

        private void ResetComboScore()
        {
            if (_comboScore <= 0)
            {
                return;
            }

            _comboScore = 0;
            PublishScoreChanged(0, 0, 0, false);
        }

        private int ResolveComboBonus() => Mathf.Max(0, _comboScore - 1);

        private void PublishScoreChanged() => PublishScoreChanged(0, 0, 0, false);

        private void PublishScoreChanged(
            int scoreDelta,
            int baseScoreDelta,
            int comboBonusDelta,
            bool isComboLandingScore)
        {
            _eventBus.Publish(new ScoreChangedEvent(
                _score,
                _highScore,
                _comboScore,
                scoreDelta,
                baseScoreDelta,
                comboBonusDelta,
                isComboLandingScore));
        }
    }
}
