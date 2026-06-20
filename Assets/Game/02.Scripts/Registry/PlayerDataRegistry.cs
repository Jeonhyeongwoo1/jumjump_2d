using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Model;
using UnityEngine;

namespace JumJump.Registry
{
    public sealed class PlayerDataRegistry
    {
        public int HighScore => _model.HighScore;
        public int Gold => _model.Gold;
        public int SelectedPlayerSkinId => _model.SelectedPlayerSkinId;

        private readonly IEventBus _eventBus;
        private readonly GameConfigData _configData;
        private readonly PlayerModel _model = new PlayerModel();
        private bool _isLoaded;

        public PlayerDataRegistry(IEventBus eventBus, GameConfigData configData)
        {
            _eventBus = eventBus;
            _configData = configData;
        }

        public void Load()
        {
            if (_isLoaded)
            {
                return;
            }

            _model.SetHighScore(PlayerPrefs.GetInt(_configData.HighScoreKey, 0));
            _model.SetGold(PlayerPrefs.GetInt(_configData.GoldKey, 0));
            _model.SetSelectedPlayerSkinId(PlayerPrefs.GetInt(
                _configData.SelectedPlayerSkinKey,
                (int)PlayerSkinType.Player_1));
            _isLoaded = true;
        }

        public bool TryUpdateHighScore(int score)
        {
            if (!_model.TryUpdateHighScore(score))
            {
                return false;
            }

            return true;
        }

        public bool AddGold(int amount)
        {
            if (!_model.AddGold(amount))
            {
                return false;
            }

            return true;
        }

        public void SetSelectedPlayerSkinId(int skinId)
        {
            _model.SetSelectedPlayerSkinId(skinId);
        }

        public void ApplyServerProgress(int highScore, int gold, int selectedPlayerSkinId)
        {
            Load();
            _model.SetHighScore(Mathf.Max(_model.HighScore, highScore));
            _model.SetGold(Mathf.Max(_model.Gold, gold));
            _model.SetSelectedPlayerSkinId(selectedPlayerSkinId);
            SaveLocal();
        }

        public void Save()
        {
            SaveLocal();
            _eventBus.Publish(new PlayerProgressSavedEvent(
                _model.HighScore,
                _model.Gold,
                _model.SelectedPlayerSkinId));
        }

        private void SaveLocal()
        {
            PlayerPrefs.SetInt(_configData.HighScoreKey, _model.HighScore);
            PlayerPrefs.SetInt(_configData.GoldKey, _model.Gold);
            PlayerPrefs.SetInt(_configData.SelectedPlayerSkinKey, _model.SelectedPlayerSkinId);
            PlayerPrefs.Save();
        }
    }
}
