using JumJump.Data;
using JumJump.Model;
using UnityEngine;

namespace JumJump.Registry
{
    public sealed class PlayerDataRegistry
    {
        public int HighScore => _model.HighScore;
        public int Gold => _model.Gold;
        public int SelectedPlayerSkinId => _model.SelectedPlayerSkinId;

        private readonly GameConfigData _configData;
        private readonly PlayerModel _model = new PlayerModel();

        public PlayerDataRegistry(GameConfigData configData)
        {
            _configData = configData;
        }

        public void Load()
        {
            _model.SetHighScore(PlayerPrefs.GetInt(_configData.HighScoreKey, 0));
            _model.SetGold(PlayerPrefs.GetInt(_configData.GoldKey, 0));
            _model.SetSelectedPlayerSkinId(PlayerPrefs.GetInt(
                _configData.SelectedPlayerSkinKey,
                (int)PlayerSkinType.Player_1));
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

        public void Save()
        {
            PlayerPrefs.SetInt(_configData.HighScoreKey, _model.HighScore);
            PlayerPrefs.SetInt(_configData.GoldKey, _model.Gold);
            PlayerPrefs.SetInt(_configData.SelectedPlayerSkinKey, _model.SelectedPlayerSkinId);
            PlayerPrefs.Save();
        }
    }
}
