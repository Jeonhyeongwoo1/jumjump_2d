using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Presenter;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Service.GameFlowState
{
    public sealed class ReadyGameFlowState : IGameFlowState, IDisposable
    {
        public GameStateType StateType => GameStateType.Ready;

        private readonly IEventBus _eventBus;
        private readonly UIService _uiService;
        private readonly UIGameScenePresenter _scenePresenter;
        private readonly ResourceConfigData _resourceConfigData;
        private bool _isSceneUiReady;
        private bool _isCountingDown;
        private float _countdownRemaining;
        private int _lastCountdownSeconds;

        public ReadyGameFlowState(
            IEventBus eventBus,
            UIService uiService,
            UIGameScenePresenter scenePresenter,
            ResourceConfigData resourceConfigData)
        {
            _eventBus = eventBus;
            _uiService = uiService;
            _scenePresenter = scenePresenter;
            _resourceConfigData = resourceConfigData;
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnResourcesReady);
        }

        public void OnEnter()
        {
            if (_isSceneUiReady)
            {
                BeginStartCountdown();
            }
        }

        public void OnUpdate(IGameFlowStateContext context)
        {
            if (!_isCountingDown)
            {
                return;
            }

            _countdownRemaining -= Time.deltaTime;
            if (_countdownRemaining <= 0f)
            {
                EndStartCountdown();
                context.ChangeState(GameStateType.Playing);
                return;
            }

            var seconds = Mathf.CeilToInt(_countdownRemaining);
            if (seconds != _lastCountdownSeconds)
            {
                _lastCountdownSeconds = seconds;
                _scenePresenter.SetStartCountdown(seconds);
            }

            _scenePresenter.SetStartCountdownProgress(ResolveCountdownProgress(seconds));
        }

        public void OnExit()
        {
            EndStartCountdown();
        }

        public void OnTapRequested(IGameFlowStateContext context)
        {
        }

        public void OnPlayerMissedLanding(IGameFlowStateContext context)
        {
            context.ChangeState(GameStateType.GameOver);
        }

        public void OnRestartRequested(IGameFlowStateContext context)
        {
            context.ChangeState(GameStateType.Ready);
            _eventBus.Publish(new GameResetEvent());
            BeginStartCountdown();
        }

        public void OnReviveRequested(IGameFlowStateContext context)
        {
        }

        private void OnResourcesReady(in GameResourcesReadyEvent ev)
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            var view = _uiService.Create<UI_GameScene>(_resourceConfigData.GameSceneUiAddressableKey);
            _scenePresenter.Bind(view);
            _isSceneUiReady = true;
            BeginStartCountdown();
        }

        private void BeginStartCountdown()
        {
            _countdownRemaining = GameConst.UI.StartCountdownSeconds;
            _lastCountdownSeconds = GameConst.UI.StartCountdownSeconds;
            _isCountingDown = true;
            _scenePresenter.ShowStartCountdown(_lastCountdownSeconds);
        }

        private float ResolveCountdownProgress(int seconds)
        {
            return Mathf.Clamp01(seconds - _countdownRemaining);
        }

        private void EndStartCountdown()
        {
            if (!_isCountingDown)
            {
                return;
            }

            _isCountingDown = false;
            _countdownRemaining = 0f;
            _lastCountdownSeconds = 0;
            _scenePresenter.HideStartCountdown();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnResourcesReady);
            EndStartCountdown();
        }
    }
}
