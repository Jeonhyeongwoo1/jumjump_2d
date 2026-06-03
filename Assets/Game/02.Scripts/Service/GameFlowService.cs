using System;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class GameFlowService : IInitializable, IDisposable
    {
        public GameStateType State => _state;

        private GameStateType _state = GameStateType.Ready;
        private readonly IEventBus _eventBus;
        private readonly ScoreService _scoreService;

        public GameFlowService(IEventBus eventBus, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            PublishStateChanged();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
        }

        private void OnTapRequested(in TapRequestedEvent ev)
        {
            if (_state == GameStateType.Ready)
            {
                _state = GameStateType.Playing;
                PublishStateChanged();
                _eventBus.Publish(new GameStartedEvent());
                return;
            }

            if (_state == GameStateType.Playing)
            {
                _eventBus.Publish(new PlayerJumpRequestedEvent());
                return;
            }

            if (_state == GameStateType.GameOver)
            {
                _state = GameStateType.Ready;
                PublishStateChanged();
                _eventBus.Publish(new RestartRequestedEvent());
            }
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            if (_state == GameStateType.GameOver)
            {
                return;
            }

            _state = GameStateType.GameOver;
            PublishStateChanged();
            _eventBus.Publish(new GameOverEvent(_scoreService.Score, _scoreService.HighScore));
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            _state = GameStateType.Ready;
            PublishStateChanged();
            _eventBus.Publish(new GameResetEvent());
        }

        private void PublishStateChanged()
        {
            Debug.Log($"[{nameof(GameFlowService)}] State changed: {_state}");
            _eventBus.Publish(new GameStateChangedEvent(_state));
        }
    }
}
