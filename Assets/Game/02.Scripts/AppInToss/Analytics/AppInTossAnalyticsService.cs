using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using JumJump.Bridge;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class AppInTossAnalyticsService : IInitializable, IDisposable
    {
        private const string UnknownUserId = "unknown";
        private const string FirstLoginMillisKeyPrefix = "appintoss_first_login_millis_";
        private const string RetentionCheckpointKeyPrefix = "appintoss_retention_checkpoint_";
        private const string FirstRunStartKeyPrefix = "appintoss_first_run_start_";
        private const string FirstRunEndKeyPrefix = "appintoss_first_run_end_";

        private readonly IEventBus _eventBus;
        private readonly AuthRegistry _authRegistry;
        private readonly ScoreService _scoreService;
        private readonly string _sessionId = Guid.NewGuid().ToString("N");

        private int _runIndex;
        private int _sessionRestartClickCount;
        private int _runLandingCount;
        private int _lastScoreMilestone;
        private int _pendingGameOverScore;
        private int _pendingGameOverHighScore;
        private float _runStartedAt;
        private bool _isRunActive;
        private bool _isContinuingAfterRevive;
        private bool _reviveUsedInRun;
        private bool _hasPendingGameOver;
        private bool _hasTrackedSessionStart;

        [Inject]
        public AppInTossAnalyticsService(IEventBus eventBus, AuthRegistry authRegistry, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _authRegistry = authRegistry;
            _scoreService = scoreService;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<AuthLoginCompletedEvent>(HandleAuthLoginCompleted);
            _eventBus.Subscribe<GameStartedEvent>(HandleGameStarted);
            _eventBus.Subscribe<GameOverEvent>(HandleGameOver);
            _eventBus.Subscribe<GameOverResultViewRequestedEvent>(HandleGameOverResultViewRequested);
            _eventBus.Subscribe<PlayerLandedEvent>(HandlePlayerLanded);
            _eventBus.Subscribe<GameRevivedEvent>(HandleGameRevived);
            _eventBus.Subscribe<ReviveOfferShownEvent>(HandleReviveOfferShown);
            _eventBus.Subscribe<ReviveAdClickedEvent>(HandleReviveAdClicked);
            _eventBus.Subscribe<RestartClickedEvent>(HandleRestartClicked);
            _eventBus.Subscribe<ScoreChangedEvent>(HandleScoreChanged);
            _eventBus.Subscribe<IAPEventLoggedEvent>(HandleIAPEventLogged);
            _eventBus.Subscribe<AdEventLoggedEvent>(HandleAdEventLogged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<AuthLoginCompletedEvent>(HandleAuthLoginCompleted);
            _eventBus.Unsubscribe<GameStartedEvent>(HandleGameStarted);
            _eventBus.Unsubscribe<GameOverEvent>(HandleGameOver);
            _eventBus.Unsubscribe<GameOverResultViewRequestedEvent>(HandleGameOverResultViewRequested);
            _eventBus.Unsubscribe<PlayerLandedEvent>(HandlePlayerLanded);
            _eventBus.Unsubscribe<GameRevivedEvent>(HandleGameRevived);
            _eventBus.Unsubscribe<ReviveOfferShownEvent>(HandleReviveOfferShown);
            _eventBus.Unsubscribe<ReviveAdClickedEvent>(HandleReviveAdClicked);
            _eventBus.Unsubscribe<RestartClickedEvent>(HandleRestartClicked);
            _eventBus.Unsubscribe<ScoreChangedEvent>(HandleScoreChanged);
            _eventBus.Unsubscribe<IAPEventLoggedEvent>(HandleIAPEventLogged);
            _eventBus.Unsubscribe<AdEventLoggedEvent>(HandleAdEventLogged);
        }

        private void HandleAuthLoginCompleted(in AuthLoginCompletedEvent ev)
        {
            if (_hasTrackedSessionStart)
            {
                return;
            }

            _hasTrackedSessionStart = true;
            var firstLoginMillis = ResolveFirstLoginMillis();
            var daysSinceFirstLogin = ResolveDaysSinceFirstLogin(firstLoginMillis);
            LogEvent("session_start", BuildSessionParams(firstLoginMillis, daysSinceFirstLogin));
            TryLogRetentionCheckpoint(firstLoginMillis, daysSinceFirstLogin);
        }

        private void HandleGameStarted(in GameStartedEvent ev)
        {
            if (_isContinuingAfterRevive)
            {
                _isContinuingAfterRevive = false;
                LogEvent(
                    "game_start",
                    $"\"run_index\":{_runIndex},\"is_revive_resume\":true,\"score\":{_scoreService.Score}");
                return;
            }

            BeginRun();
            LogEvent(
                "game_start",
                $"\"run_index\":{_runIndex},\"is_revive_resume\":false,\"score\":{_scoreService.Score}");
            TryLogFirstRunStart();
        }

        private void HandleGameOver(in GameOverEvent ev)
        {
            _pendingGameOverScore = ev.Score;
            _pendingGameOverHighScore = ev.HighScore;
            _hasPendingGameOver = true;
            LogEvent(
                "game_over",
                $"\"run_index\":{_runIndex},\"score\":{ev.Score},\"high_score\":{ev.HighScore},\"landing_count\":{_runLandingCount},\"revive_used\":{FormatBool(_reviveUsedInRun)}");
        }

        private void HandleGameOverResultViewRequested(in GameOverResultViewRequestedEvent ev)
        {
            if (!_isRunActive && !_hasPendingGameOver)
            {
                return;
            }

            var score = _hasPendingGameOver ? _pendingGameOverScore : _scoreService.Score;
            var highScore = _hasPendingGameOver ? _pendingGameOverHighScore : _scoreService.HighScore;
            EndRun(score, highScore);
        }

        private void HandlePlayerLanded(in PlayerLandedEvent ev)
        {
            if (!_isRunActive)
            {
                return;
            }

            _runLandingCount++;
        }

        private void HandleGameRevived(in GameRevivedEvent ev)
        {
            _reviveUsedInRun = true;
            _isContinuingAfterRevive = true;
            _hasPendingGameOver = false;
            LogEvent(
                "revive_success",
                $"\"run_index\":{_runIndex},\"score\":{_scoreService.Score},\"landing_count\":{_runLandingCount}");
        }

        private void HandleReviveOfferShown(in ReviveOfferShownEvent ev)
        {
            LogEvent(
                "revive_offer_shown",
                $"\"run_index\":{_runIndex},\"score\":{_scoreService.Score},\"landing_count\":{_runLandingCount},\"revive_used\":{FormatBool(_reviveUsedInRun)}");
        }

        private void HandleReviveAdClicked(in ReviveAdClickedEvent ev)
        {
            LogEvent(
                "revive_ad_clicked",
                $"\"run_index\":{_runIndex},\"score\":{_scoreService.Score},\"landing_count\":{_runLandingCount}");
        }

        private void HandleRestartClicked(in RestartClickedEvent ev)
        {
            _sessionRestartClickCount++;
            LogEvent(
                "restart_clicked",
                $"\"restart_count_in_session\":{_sessionRestartClickCount},\"run_index\":{_runIndex},\"score\":{_scoreService.Score}");
        }

        private void HandleScoreChanged(in ScoreChangedEvent ev)
        {
            var milestone = ResolveScoreMilestone(ev.Score);
            if (milestone <= _lastScoreMilestone)
            {
                return;
            }

            _lastScoreMilestone = milestone;
            LogEvent(
                "score_milestone",
                $"\"run_index\":{_runIndex},\"score_bucket\":{milestone},\"score\":{ev.Score},\"high_score\":{ev.HighScore},\"landing_count\":{_runLandingCount}");
        }

        private void HandleIAPEventLogged(in IAPEventLoggedEvent ev)
        {
            LogEvent(
                "iap_event",
                $"\"event_type\":\"{Escape(ev.EventType)}\",\"product_id\":\"{Escape(ev.ProductId)}\",\"order_id\":\"{Escape(ev.OrderId)}\",\"error\":\"{Escape(ev.Error)}\",\"score\":{_scoreService.Score}");
        }

        private void HandleAdEventLogged(in AdEventLoggedEvent ev)
        {
            string rewardParams = ev.HasReward
                ? $",\"reward_type\":\"{Escape(ev.RewardType)}\",\"reward_amount\":{ev.RewardAmount}"
                : string.Empty;
            string errorParam = string.IsNullOrEmpty(ev.Error)
                ? string.Empty
                : $",\"error\":\"{Escape(ev.Error)}\"";
            LogEvent(
                "ad_event",
                $"\"ad_group_id\":\"{Escape(ev.AdGroupId)}\",\"placement\":\"{Escape(ev.AdGroupId)}\",\"ad_type\":\"rewarded\",\"event_type\":\"{Escape(ev.EventType)}\",\"score\":{ev.Score}{errorParam}{rewardParams}");
        }

        private void BeginRun()
        {
            _runIndex++;
            _runLandingCount = 0;
            _lastScoreMilestone = 0;
            _pendingGameOverScore = 0;
            _pendingGameOverHighScore = 0;
            _runStartedAt = Time.realtimeSinceStartup;
            _isRunActive = true;
            _reviveUsedInRun = false;
            _hasPendingGameOver = false;
        }

        private void EndRun(int score, int highScore)
        {
            var durationSeconds = _isRunActive
                ? Mathf.Max(0f, Time.realtimeSinceStartup - _runStartedAt)
                : 0f;
            var runParams = BuildRunEndParams(score, highScore, durationSeconds);
            LogEvent("run_end", runParams);
            TryLogFirstRunEnd(runParams);

            _isRunActive = false;
            _isContinuingAfterRevive = false;
            _reviveUsedInRun = false;
            _hasPendingGameOver = false;
        }

        private void TryLogFirstRunStart()
        {
            if (HasUserFlag(FirstRunStartKeyPrefix))
            {
                return;
            }

            SetUserFlag(FirstRunStartKeyPrefix);
            var daysSinceFirstLogin = ResolveDaysSinceFirstLogin(ResolveFirstLoginMillis());
            LogEvent(
                "first_run_start",
                $"\"run_index\":{_runIndex},\"retention_day\":{daysSinceFirstLogin},\"high_score\":{_scoreService.HighScore},\"gold\":{_scoreService.Gold}");
        }

        private void TryLogFirstRunEnd(string runParams)
        {
            if (HasUserFlag(FirstRunEndKeyPrefix))
            {
                return;
            }

            SetUserFlag(FirstRunEndKeyPrefix);
            LogEvent("first_run_end", runParams);
        }

        private void TryLogRetentionCheckpoint(long firstLoginMillis, int daysSinceFirstLogin)
        {
            if (daysSinceFirstLogin < 1 || daysSinceFirstLogin > 7)
            {
                return;
            }

            var key = $"{RetentionCheckpointKeyPrefix}{GetUserId()}_{daysSinceFirstLogin}";
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return;
            }

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            LogEvent("retention_checkpoint", BuildSessionParams(firstLoginMillis, daysSinceFirstLogin));
        }

        private string BuildSessionParams(long firstLoginMillis, int daysSinceFirstLogin)
        {
            return $"\"is_new_user\":{FormatBool(daysSinceFirstLogin == 0)},\"days_since_first_login\":{daysSinceFirstLogin},\"retention_day\":{daysSinceFirstLogin},\"created_at_millis\":{firstLoginMillis},\"high_score\":{_scoreService.HighScore},\"gold\":{_scoreService.Gold},\"has_removed_ads\":{FormatBool(_authRegistry.HasRemovedAds)}";
        }

        private string BuildRunEndParams(int score, int highScore, float durationSeconds)
        {
            return $"\"run_index\":{_runIndex},\"score\":{score},\"high_score\":{highScore},\"duration_sec\":{FormatFloat(durationSeconds)},\"landing_count\":{_runLandingCount},\"revive_used\":{FormatBool(_reviveUsedInRun)},\"restart_count_in_session\":{_sessionRestartClickCount}";
        }

        private int ResolveScoreMilestone(int score)
        {
            if (score >= GameConst.Score.ScoreMilestoneLegend)
            {
                return GameConst.Score.ScoreMilestoneLegend;
            }

            if (score >= GameConst.Score.ScoreMilestoneHuge)
            {
                return GameConst.Score.ScoreMilestoneHuge;
            }

            if (score >= GameConst.Score.ScoreMilestoneLarge)
            {
                return GameConst.Score.ScoreMilestoneLarge;
            }

            if (score >= GameConst.Score.ScoreMilestoneMedium)
            {
                return GameConst.Score.ScoreMilestoneMedium;
            }

            if (score >= GameConst.Score.ScoreMilestoneSmall)
            {
                return GameConst.Score.ScoreMilestoneSmall;
            }

            return 0;
        }

        private long ResolveFirstLoginMillis()
        {
            if (_authRegistry.CreatedAtMillis > 0)
            {
                return _authRegistry.CreatedAtMillis;
            }

            var key = FirstLoginMillisKeyPrefix + GetUserId();
            if (PlayerPrefs.HasKey(key) &&
                long.TryParse(PlayerPrefs.GetString(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var storedMillis) &&
                storedMillis > 0)
            {
                return storedMillis;
            }

            var nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PlayerPrefs.SetString(key, nowMillis.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            return nowMillis;
        }

        private int ResolveDaysSinceFirstLogin(long firstLoginMillis)
        {
            if (firstLoginMillis <= 0)
            {
                return 0;
            }

            var firstLoginDate = DateTimeOffset.FromUnixTimeMilliseconds(firstLoginMillis).UtcDateTime.Date;
            var days = (DateTime.UtcNow.Date - firstLoginDate).Days;
            return Mathf.Max(0, days);
        }

        private bool HasUserFlag(string keyPrefix)
        {
            return PlayerPrefs.GetInt(keyPrefix + GetUserId(), 0) == 1;
        }

        private void SetUserFlag(string keyPrefix)
        {
            PlayerPrefs.SetInt(keyPrefix + GetUserId(), 1);
            PlayerPrefs.Save();
        }

        private void LogEvent(string logName, string paramsJson)
        {
            LogEventAsync(logName, paramsJson).Forget();
        }

        private UniTask LogEventAsync(string logName, string paramsJson)
        {
            string payloadJson = $"{{\"Log_name\":\"{Escape(logName)}\",\"Log_type\":\"event\",\"Params\":{{\"user_id\":\"{Escape(GetUserId())}\",\"session_id\":\"{_sessionId}\",{paramsJson}}}}}";
#if UNITY_WEBGL && !UNITY_EDITOR
            AppInTossAnalyticsWebGL.EventLog(payloadJson);
#else
            Debug.Log($"[{nameof(AppInTossAnalyticsService)}] {payloadJson}");
#endif
            return UniTask.CompletedTask;
        }

        private string GetUserId()
        {
            return !string.IsNullOrEmpty(_authRegistry.UserId) ? _authRegistry.UserId : UnknownUserId;
        }

        private string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
