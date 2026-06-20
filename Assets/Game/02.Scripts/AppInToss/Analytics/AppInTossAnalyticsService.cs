using System;
using Cysharp.Threading.Tasks;
using JumJump.Bridge;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class AppInTossAnalyticsService : IInitializable, IDisposable
    {
        private const string UnknownUserId = "unknown";
        private readonly IEventBus _eventBus;
        private readonly AuthRegistry _authRegistry;
        private readonly ScoreService _scoreService;
        private readonly string _sessionId = Guid.NewGuid().ToString("N");
        private bool _hasTrackedGameStart;
        private bool _hasTrackedGameOver;

        public AppInTossAnalyticsService(IEventBus eventBus, AuthRegistry authRegistry, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _authRegistry = authRegistry;
            _scoreService = scoreService;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GameStartedEvent>(HandleGameStarted);
            _eventBus.Subscribe<GameOverEvent>(HandleGameOver);
            _eventBus.Subscribe<ScoreChangedEvent>(HandleScoreChanged);
            _eventBus.Subscribe<IAPEventLoggedEvent>(HandleIAPEventLogged);
            _eventBus.Subscribe<AdEventLoggedEvent>(HandleAdEventLogged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameStartedEvent>(HandleGameStarted);
            _eventBus.Unsubscribe<GameOverEvent>(HandleGameOver);
            _eventBus.Unsubscribe<ScoreChangedEvent>(HandleScoreChanged);
            _eventBus.Unsubscribe<IAPEventLoggedEvent>(HandleIAPEventLogged);
            _eventBus.Unsubscribe<AdEventLoggedEvent>(HandleAdEventLogged);
        }

        private void HandleGameStarted(in GameStartedEvent ev)
        {
            if (_hasTrackedGameStart)
            {
                return;
            }

            _hasTrackedGameStart = true;
            _hasTrackedGameOver = false;
            LogEvent("game_start", $"\"score\":{_scoreService.Score}");
        }

        private void HandleGameOver(in GameOverEvent ev)
        {
            if (_hasTrackedGameOver)
            {
                return;
            }

            _hasTrackedGameOver = true;
            LogEvent("game_over", $"\"score\":{ev.Score},\"high_score\":{ev.HighScore}");
        }

        private void HandleScoreChanged(in ScoreChangedEvent ev)
        {
            LogEvent("score_changed", $"\"score\":{ev.Score},\"high_score\":{ev.HighScore}");
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

        private string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
