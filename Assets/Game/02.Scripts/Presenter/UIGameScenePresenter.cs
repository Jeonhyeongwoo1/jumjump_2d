using System;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Service;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UIGameScenePresenter : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ScoreService _scoreService;
        private readonly PlayerDataRegistry _playerDataRegistry;
        private readonly PlayerRegistry _playerRegistry;
        private UI_GameScene _view;

        public UIGameScenePresenter(
            IEventBus eventBus,
            ScoreService scoreService,
            PlayerDataRegistry playerDataRegistry,
            PlayerRegistry playerRegistry)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
            _playerDataRegistry = playerDataRegistry;
            _playerRegistry = playerRegistry;
        }

        public void Bind(UI_GameScene view)
        {
            if (view == null)
            {
                Debug.LogError($"[{nameof(UIGameScenePresenter)}] Missing view.");
                return;
            }

            _view = view;
            _view.AddEvents(OnGameReadyClicked, OnCharacterSelected);
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            _view.SetScore(_scoreService.Score);
            _view.SetGold(_scoreService.Gold);
            _view.SetSelectedCharacter(_playerDataRegistry.SelectedPlayerSkinId);
            _view.ShowReady();
        }

        public void Unbind()
        {
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            _view?.RemoveEvents();
            _view = null;
        }

        public void Dispose()
        {
            Unbind();
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            if (_view == null)
            {
                return;
            }

            if (ev.ScoreDelta > 0)
            {
                _view.SetScoreAnimated(ev.Score);
                return;
            }

            _view.SetScore(ev.Score);
        }

        private void OnGoldChanged(in GoldChangedEvent ev)
        {
            _view?.SetGold(ev.Gold);
        }

        private void OnGameReadyClicked()
        {
            _eventBus.Publish(new TapRequestedEvent());
        }

        private void OnCharacterSelected(int skinId)
        {
            _playerDataRegistry.SetSelectedPlayerSkinId(skinId);
            _playerDataRegistry.Save();
            _view?.SetSelectedCharacter(skinId);
            ApplySelectedPlayerSkin(skinId);
        }

        private void ApplySelectedPlayerSkin(int skinId)
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            if (!player.TryApplySkin(skinId))
            {
                Debug.LogError($"[{nameof(UIGameScenePresenter)}] Failed to apply selected player skin: {skinId}");
            }
        }

        public void ShowReady()
        {
            _view?.ShowReady();
        }

        public void ShowScorePanel()
        {
            _view?.ShowScorePanel();
        }

        public void HideGameReadyPanel()
        {
            _view?.HideGameReadyPanel();
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
