using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class ScoreService : IInitializable, ITickable, IDisposable
    {
        public int Score => _score;
        public int BaseScore => _baseScore;
        public int HighScore => _playerDataRegistry.HighScore;
        public int RoundHighScoreTarget => _roundHighScoreTarget;
        public int ComboScore => _comboScore;
        public int Gold => _playerDataRegistry.Gold;
        public int AdRewardGoldAmount => _configData.AdRewardGoldAmount;

        private int _score;
        private int _baseScore;
        private int _roundHighScoreTarget;
        private int _comboScore;
        private int _comboScoreProgress;
        private int _pendingAnimatedScore;
        private float _pendingAnimatedScoreElapsed;
        private readonly IEventBus _eventBus;
        private readonly GameConfigData _configData;
        private readonly PlayerDataRegistry _playerDataRegistry;

        public ScoreService(
            IEventBus eventBus,
            GameConfigData configData,
            PlayerDataRegistry playerDataRegistry)
        {
            _eventBus = eventBus;
            _configData = configData;
            _playerDataRegistry = playerDataRegistry;
        }

        public void Initialize()
        {
            _playerDataRegistry.Load();
            _roundHighScoreTarget = HighScore;
            _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<RocketBoostPlatformsPassedEvent>(OnRocketBoostPlatformsPassed);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            _eventBus.Subscribe<GameOverResultViewRequestedEvent>(OnGameOverResultViewRequested);
            PublishScoreChanged();
            PublishGoldChanged(0);
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
            _eventBus.Unsubscribe<GameOverResultViewRequestedEvent>(OnGameOverResultViewRequested);
        }

        public bool GrantAdRewardGold()
        {
            if (!AddGold(_configData.AdRewardGoldAmount, GoldChangeSourceType.AdReward))
            {
                return false;
            }

            _playerDataRegistry.Save();
            return true;
        }

        private void OnPlayerLanded(in PlayerLandedEvent ev)
        {
            var wasComboActive = _comboScore > 0;
            var comboBonus = UpdateComboScore(ev);
            var baseScore = Mathf.Max(0, _configData.ScorePerLanding);
            var scoreDelta = baseScore + comboBonus;
            AddBaseScore(baseScore);
            AddScore(scoreDelta, baseScore, comboBonus, comboBonus > 0);
            AddGold(1);

            if (comboBonus > 0)
            {
                _eventBus.Publish(new ComboPlatformActivatedEvent(
                    ev.Platform,
                    ev.LandingPosition,
                    comboBonus,
                    scoreDelta,
                    !wasComboActive));
            }
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            ResetComboScore();
        }

        private void OnRocketBoostPlatformsPassed(in RocketBoostPlatformsPassedEvent ev)
        {
            var platformCount = Mathf.Max(0, ev.PlatformCount);
            QueueAnimatedScore(platformCount * Mathf.Max(0, _configData.ScorePerLanding));
            AddGold(platformCount);
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
            _playerDataRegistry.TryUpdateHighScore(_score);

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

        private bool AddGold(
            int amount,
            GoldChangeSourceType source = GoldChangeSourceType.Gameplay)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (_playerDataRegistry.AddGold(amount))
            {
                PublishGoldChanged(amount, source);
                return true;
            }

            return false;
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            if (_comboScore > 0 || _comboScoreProgress > 0)
            {
                _eventBus.Publish(new ComboEndedEvent());
            }

            _pendingAnimatedScore = 0;
            _pendingAnimatedScoreElapsed = 0f;
            _score = 0;
            _baseScore = 0;
            _roundHighScoreTarget = HighScore;
            _comboScore = 0;
            _comboScoreProgress = 0;
            PublishScoreChanged(0, 0, 0, false);
        }

        private void OnGameOverResultViewRequested(in GameOverResultViewRequestedEvent ev)
        {
            _playerDataRegistry.Save();
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
            _eventBus.Publish(new ComboEndedEvent());
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

        private void PublishScoreChanged() => PublishScoreChanged(0, 0, 0, false);

        private void PublishGoldChanged(
            int goldDelta,
            GoldChangeSourceType source = GoldChangeSourceType.Gameplay)
        {
            _eventBus.Publish(new GoldChangedEvent(Gold, goldDelta, source));
        }

        private void PublishScoreChanged(
            int scoreDelta,
            int baseScoreDelta,
            int comboBonusDelta,
            bool isComboLandingScore)
        {
            _eventBus.Publish(new ScoreChangedEvent(
                _score,
                HighScore,
                _comboScore,
                scoreDelta,
                baseScoreDelta,
                comboBonusDelta,
                isComboLandingScore));
        }
    }
}
