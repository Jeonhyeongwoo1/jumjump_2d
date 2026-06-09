using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;

namespace JumJump.Service.GameFlowState
{
    public sealed class PlayingGameFlowState : IGameFlowState
    {
        public GameStateType StateType => GameStateType.Playing;

        private readonly IEventBus _eventBus;
        private readonly PlayerConfigData _configData;
        private bool _isGameOverPending;
        private float _gameOverDelayElapsed;

        public PlayingGameFlowState(IEventBus eventBus, PlayerConfigData configData)
        {
            _eventBus = eventBus;
            _configData = configData;
        }

        public void OnEnter()
        {
            _isGameOverPending = false;
            _gameOverDelayElapsed = 0f;
            _eventBus.Publish(new GameStartedEvent());
        }

        public void OnUpdate(IGameFlowStateContext context)
        {
            if (!_isGameOverPending)
            {
                return;
            }

            _gameOverDelayElapsed += Time.deltaTime;
            if (_gameOverDelayElapsed >= Mathf.Max(0f, _configData.PlayerGameOverPopupDelay))
            {
                context.ChangeState(GameStateType.GameOver);
            }
        }

        public void OnExit()
        {
            _isGameOverPending = false;
            _gameOverDelayElapsed = 0f;
        }

        public void OnTapRequested(IGameFlowStateContext context)
        {
            if (_isGameOverPending)
            {
                return;
            }

            _eventBus.Publish(new PlayerJumpRequestedEvent());
        }

        public void OnPlayerMissedLanding(IGameFlowStateContext context)
        {
            if (_isGameOverPending)
            {
                return;
            }

            _isGameOverPending = true;
            _gameOverDelayElapsed = 0f;
        }

        public void OnRestartRequested(IGameFlowStateContext context)
        {
            _isGameOverPending = false;
            _gameOverDelayElapsed = 0f;
            context.ChangeState(GameStateType.Ready);
            _eventBus.Publish(new GameResetEvent());
        }
    }
}
