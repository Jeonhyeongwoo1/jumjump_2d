using System;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Service;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UIGameScenePresenter : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ScoreService _scoreService;
        private UI_GameScene _view;

        public UIGameScenePresenter(IEventBus eventBus, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
        }

        public void Bind(UI_GameScene view)
        {
            if (view == null)
            {
                Debug.LogError($"[{nameof(UIGameScenePresenter)}] Missing view.");
                return;
            }

            _view = view;
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _view.SetScore(_scoreService.Score);
            _view.SetComboScore(_scoreService.ComboScore);
            _view.HideStartCountdown();
        }

        public void Unbind()
        {
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _view = null;
        }

        public void Dispose()
        {
            Unbind();
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            _view?.SetScore(ev.Score);
            _view?.SetComboScore(ev.ComboScore);
        }

        public void ShowStartCountdown(int seconds)
        {
            _view?.ShowStartCountdown(seconds);
        }

        public void SetStartCountdown(int seconds)
        {
            _view?.SetStartCountdown(seconds);
        }

        public void SetStartCountdownProgress(float normalized)
        {
            _view?.SetStartCountdownProgress(normalized);
        }

        public void HideStartCountdown()
        {
            _view?.HideStartCountdown();
        }
    }
}
