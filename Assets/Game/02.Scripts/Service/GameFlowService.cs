using System;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Service.GameFlowState;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class GameFlowService : IInitializable, ITickable, IDisposable, IGameFlowStateContext
    {
        public GameStateType State => _currentState != null ? _currentState.StateType : GameStateType.Ready;

        private bool _isReady;
        private IGameFlowState _currentState;
        private readonly IEventBus _eventBus;
        private readonly ReadyGameFlowState _readyState;
        private readonly PlayingGameFlowState _playingState;
        private readonly GameOverGameFlowState _gameOverState;

        public GameFlowService(
            IEventBus eventBus,
            ReadyGameFlowState readyState,
            PlayingGameFlowState playingState,
            GameOverGameFlowState gameOverState)
        {
            _eventBus = eventBus;
            _readyState = readyState;
            _playingState = playingState;
            _gameOverState = gameOverState;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Subscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<RestartRequestedEvent>(OnRestartRequested);
            ChangeState(GameStateType.Ready);
        }

        public void Tick()
        {
            _currentState?.OnUpdate();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            _eventBus.Unsubscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<RestartRequestedEvent>(OnRestartRequested);
            _currentState?.OnExit();
            _currentState = null;
        }

        public void ChangeState(GameStateType stateType)
        {
            var nextState = GetState(stateType);
            if (nextState == null)
            {
                Debug.LogError($"[{nameof(GameFlowService)}] Missing state: {stateType}.");
                return;
            }

            if (_currentState == nextState)
            {
                PublishStateChanged();
                return;
            }

            _currentState?.OnExit();
            _currentState = nextState;
            PublishStateChanged();
            _currentState.OnEnter();
        }

        private void OnResourcesReady(in GameResourcesReadyEvent ev)
        {
            _isReady = true;
        }

        private void OnTapRequested(in TapRequestedEvent ev)
        {
            if (!_isReady)
            {
                return;
            }

            _currentState?.OnTapRequested(this);
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            _currentState?.OnPlayerMissedLanding(this);
        }

        private void OnRestartRequested(in RestartRequestedEvent ev)
        {
            _currentState?.OnRestartRequested(this);
        }

        private IGameFlowState GetState(GameStateType stateType)
        {
            switch (stateType)
            {
                case GameStateType.Ready:
                    return _readyState;
                case GameStateType.Playing:
                    return _playingState;
                case GameStateType.GameOver:
                    return _gameOverState;
                default:
                    return null;
            }
        }

        private void PublishStateChanged()
        {
            Debug.Log($"[{nameof(GameFlowService)}] State changed: {State}");
            _eventBus.Publish(new GameStateChangedEvent(State));
        }
    }
}
