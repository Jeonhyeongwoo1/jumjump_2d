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
        private int _comboScoreProgress;
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

        public bool ShouldPreviewHighScoreBoardOnNextLanding()
        {
            if (_highScore <= GameConst.Score.ScoreBoardMinimumHighScore || _score >= _highScore)
            {
                return false;
            }

            var baseScore = Mathf.Max(0, _configData.ScorePerLanding);
            var predictedScore = _score + baseScore + ResolveCurrentComboScoreForLanding();
            return _highScore - predictedScore <= GameConst.Score.ScoreBoardPreviewRemainingScore;
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            var comboBonus = UpdateComboScore(ev);
            var baseScore = Mathf.Max(0, _configData.ScorePerLanding);
            AddBaseScore(baseScore);
            AddScore(baseScore + comboBonus, baseScore, comboBonus, comboBonus > 0);
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
            _comboScoreProgress = 0;
            PublishScoreChanged(0, 0, 0, false);
        }

        private int UpdateComboScore(in PlayerLandedEvent ev)
        {
            if (ev.Platform == null)
            {
                ResetComboScore();
                return 0;
            }

            var tolerance = Mathf.Max(0f, _configData.ComboLandingCenterTolerance);
            if (Mathf.Abs(ev.Platform.CenterX - ev.LandingPosition.x) <= tolerance)
            {
                return AdvanceComboScore();
            }

            ResetComboScore();
            return 0;
        }

        private void ResetComboScore()
        {
            if (_comboScore <= 0 && _comboScoreProgress <= 0)
            {
                return;
            }

            _comboScore = 0;
            _comboScoreProgress = 0;
            PublishScoreChanged(0, 0, 0, false);
        }

        private int AdvanceComboScore()
        {
            if (_comboScore <= 0)
            {
                _comboScore = 1;
                _comboScoreProgress = 0;
            }

            var comboScoreForLanding = _comboScore;
            _comboScoreProgress++;
            if (_comboScoreProgress >= _comboScore)
            {
                _comboScore++;
                _comboScoreProgress = 0;
            }

            return comboScoreForLanding;
        }

        private int ResolveCurrentComboScoreForLanding()
        {
            if (_comboScore <= 0)
            {
                return 1;
            }

            return _comboScore;
        }

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
