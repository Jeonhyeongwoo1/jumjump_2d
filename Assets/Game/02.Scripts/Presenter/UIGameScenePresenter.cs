using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Service;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UIGameScenePresenter : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ScoreService _scoreService;
        private readonly PlayerDataRegistry _playerDataRegistry;
        private readonly PlayerRegistry _playerRegistry;
        private readonly ResourceService _resourceService;
        private readonly LocalizationService _localizationService;
        private readonly ResourceConfigData _resourceConfigData;
        private UI_GameScene _view;
        private bool _hasPlayedBestScoreAnimation;

        public UIGameScenePresenter(
            IEventBus eventBus,
            ScoreService scoreService,
            PlayerDataRegistry playerDataRegistry,
            PlayerRegistry playerRegistry,
            ResourceService resourceService,
            LocalizationService localizationService,
            ResourceConfigData resourceConfigData)
        {
            _eventBus = eventBus;
            _scoreService = scoreService;
            _playerDataRegistry = playerDataRegistry;
            _playerRegistry = playerRegistry;
            _resourceService = resourceService;
            _localizationService = localizationService;
            _resourceConfigData = resourceConfigData;
        }

        public void Bind(UI_GameScene view)
        {
            if (view == null)
            {
                GameLogger.Error(nameof(UIGameScenePresenter), "Missing view.");
                return;
            }

            _view = view;
            _hasPlayedBestScoreAnimation = false;
            _view.AddEvents(OnGameReadyClicked, OnAdRewardClicked, OnCharacterConfirmed, OnCharacterPurchased);
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            _view.SetScore(_scoreService.Score);
            _view.SetGold(_scoreService.Gold);
            _view.SetAdRewardGoldAmount(_scoreService.AdRewardGoldAmount);
            _view.SetSelectedCharacter(_playerDataRegistry.SelectedPlayerSkinId);
            LoadCharacterPagesAsync(_view, _view.GetCancellationTokenOnDestroy()).Forget();
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

            if (ev.Score <= 0)
            {
                _hasPlayedBestScoreAnimation = false;
            }

            if (ev.ScoreDelta > 0)
            {
                if (ShouldPlayBestScoreAnimation(ev))
                {
                    _hasPlayedBestScoreAnimation = true;
                    _view.SetScoreBestScoreAnimated(ev.Score);
                    return;
                }

                _view.SetScoreAnimated(ev.Score);
                return;
            }

            _view.SetScore(ev.Score);
        }

        private bool ShouldPlayBestScoreAnimation(in ScoreChangedEvent ev)
        {
            return !_hasPlayedBestScoreAnimation &&
                   _scoreService.RoundHighScoreTarget > GameConst.Score.ScoreBoardMinimumHighScore &&
                   ev.Score > _scoreService.RoundHighScoreTarget;
        }

        private void OnGoldChanged(in GoldChangedEvent ev)
        {
            if (_view == null)
            {
                return;
            }

            _view.SetGold(ev.Gold);
            if (ev.GoldDelta > 0 && ev.Source == GoldChangeSourceType.AdReward)
            {
                _view.PlayAdRewardGoldMoveFX();
            }
        }

        private void OnGameReadyClicked()
        {
            _eventBus.Publish(new SoundRequestedEvent(GameSoundType.UiButtonTap));
            _eventBus.Publish(new TapRequestedEvent());
        }

        private void OnAdRewardClicked()
        {
            _eventBus.Publish(new SoundRequestedEvent(GameSoundType.UiButtonTap));
            _scoreService.GrantAdRewardGold();
        }

        private bool OnCharacterConfirmed(int skinId)
        {
            _eventBus.Publish(new SoundRequestedEvent(GameSoundType.UiButtonTap));
            if (!_playerDataRegistry.OwnsPlayerSkin(skinId))
            {
                return false;
            }

            _playerDataRegistry.SetSelectedPlayerSkinId(skinId);
            _playerDataRegistry.Save();
            _view?.SetSelectedCharacter(skinId);
            ApplySelectedPlayerSkin(skinId);
            return true;
        }

        private bool OnCharacterPurchased(int skinId)
        {
            _eventBus.Publish(new SoundRequestedEvent(GameSoundType.UiButtonTap));
            if (_playerDataRegistry.OwnsPlayerSkin(skinId))
            {
                return true;
            }

            if (!TryPurchaseCharacter(skinId))
            {
                return false;
            }

            _playerDataRegistry.Save();
            return true;
        }

        private bool TryPurchaseCharacter(int skinId)
        {
            if (!TryResolvePlayerSkinData(skinId, out var skinData))
            {
                GameLogger.Error(nameof(UIGameScenePresenter), $"Missing player skin data: {skinId}");
                return false;
            }

            if (!_playerDataRegistry.TryPurchasePlayerSkin(skinId, skinData.Price))
            {
                GameLogger.Info(nameof(UIGameScenePresenter), $"Not enough gold to purchase player skin: {skinId}");
                return false;
            }

            _view?.SetCharacterOwned(skinId, true);
            _eventBus.Publish(new GoldChangedEvent(
                _playerDataRegistry.Gold,
                -skinData.Price,
                GoldChangeSourceType.Purchase));
            return true;
        }

        private void ApplySelectedPlayerSkin(int skinId)
        {
            var player = _playerRegistry.Player;
            if (player == null)
            {
                return;
            }

            if (skinId == (int)PlayerSkinType.Player_1)
            {
                player.ApplyDefaultSkin();
                return;
            }

            if (!TryResolvePlayerSkinData(skinId, out var skinData) ||
                !player.TryApplySkin(skinData))
            {
                GameLogger.Error(nameof(UIGameScenePresenter), $"Failed to apply selected player skin: {skinId}");
            }
        }

        private async UniTask LoadCharacterPagesAsync(UI_GameScene view, CancellationToken cancellationToken)
        {
            var keys = _resourceConfigData.PlayerSpriteAddressableKeys;
            var skinIds = new List<int>(keys.Length);
            var sprites = new List<Sprite>(keys.Length);
            var prices = new List<int>(keys.Length);
            var owned = new List<bool>(keys.Length);
            var localizedNames = new List<string>(keys.Length);
            var localizedDescriptions = new List<string>(keys.Length);

            for (var i = 0; i < keys.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = keys[i];
                if (!TryResolveSkinId(key, out var skinId))
                {
                    continue;
                }

                var skinDataKey = ResolvePlayerSkinDataAddressableKey(skinId);
                var skinDataLoaded = await _resourceService.LoadKeyAsync(skinDataKey, cancellationToken);
                if (!skinDataLoaded)
                {
                    GameLogger.Error(nameof(UIGameScenePresenter), $"Failed to load player skin data: {skinDataKey}");
                    continue;
                }

                if (!TryResolvePlayerSkinData(skinId, out var skinData))
                {
                    GameLogger.Error(nameof(UIGameScenePresenter), $"Missing player skin data: {skinId}");
                    continue;
                }

                var loaded = await _resourceService.LoadKeyAsync(key, cancellationToken);
                if (!loaded)
                {
                    GameLogger.Error(nameof(UIGameScenePresenter), $"Failed to load character sprite: {key}");
                    continue;
                }

                var sprite = _resourceService.GetAsset<Sprite>(key);
                if (sprite == null)
                {
                    GameLogger.Error(nameof(UIGameScenePresenter), $"Missing loaded character sprite: {key}");
                    continue;
                }

                skinIds.Add(skinId);
                sprites.Add(sprite);
                prices.Add(skinData.Price);
                owned.Add(_playerDataRegistry.OwnsPlayerSkin(skinId));
                localizedNames.Add(_localizationService.GetPlayerSkinName(skinId));
                localizedDescriptions.Add(_localizationService.GetPlayerSkinDescription(skinId));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (_view != view)
            {
                return;
            }

            view.SetCharacterPages(skinIds, sprites, prices, owned, localizedNames, localizedDescriptions);
            view.SetSelectedCharacter(_playerDataRegistry.SelectedPlayerSkinId);
        }

        private bool TryResolveSkinId(string spriteAddressableKey, out int skinId)
        {
            skinId = 0;
            var delimiterIndex = spriteAddressableKey.LastIndexOf('_');
            if (delimiterIndex < 0 || delimiterIndex >= spriteAddressableKey.Length - 1)
            {
                GameLogger.Error(nameof(UIGameScenePresenter), $"Invalid character sprite key: {spriteAddressableKey}");
                return false;
            }

            if (int.TryParse(spriteAddressableKey.Substring(delimiterIndex + 1), out skinId))
            {
                return true;
            }

            GameLogger.Error(nameof(UIGameScenePresenter), $"Invalid character skin id: {spriteAddressableKey}");
            return false;
        }

        private bool TryResolvePlayerSkinData(int skinId, out PlayerSkinData skinData)
        {
            skinData = _resourceService.GetAsset<PlayerSkinData>(ResolvePlayerSkinDataAddressableKey(skinId));
            return skinData != null;
        }

        private string ResolvePlayerSkinDataAddressableKey(int skinId)
        {
            return _resourceConfigData.PlayerSkinDataAddressableKeyPrefix + skinId;
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
