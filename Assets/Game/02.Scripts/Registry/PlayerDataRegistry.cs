using System.Collections.Generic;
using System.Text;
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
            _model.SetOwnedPlayerSkinIds(ParseOwnedPlayerSkinIds(PlayerPrefs.GetString(
                _configData.PurchasedPlayerSkinsKey,
                string.Empty)));
            _model.SetSelectedPlayerSkinId(PlayerPrefs.GetInt(
                _configData.SelectedPlayerSkinKey,
                (int)PlayerSkinType.Player_1));
            _model.ValidateSelectedPlayerSkinOwnership();
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

        public bool OwnsPlayerSkin(int skinId)
        {
            return _model.OwnsPlayerSkin(skinId);
        }

        public bool TryPurchasePlayerSkin(int skinId, int price)
        {
            return _model.TryPurchasePlayerSkin(skinId, price);
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
            PlayerPrefs.SetString(_configData.PurchasedPlayerSkinsKey, SerializeOwnedPlayerSkinIds());
            PlayerPrefs.Save();
        }

        private List<int> ParseOwnedPlayerSkinIds(string value)
        {
            var skinIds = new List<int>(8);
            if (string.IsNullOrWhiteSpace(value))
            {
                return skinIds;
            }

            var tokens = value.Split(',');
            for (var i = 0; i < tokens.Length; i++)
            {
                if (int.TryParse(tokens[i], out var skinId))
                {
                    skinIds.Add(skinId);
                }
            }

            return skinIds;
        }

        private string SerializeOwnedPlayerSkinIds()
        {
            var skinIds = new List<int>(_model.OwnedPlayerSkinIds);
            skinIds.Sort();

            var builder = new StringBuilder();
            for (var i = 0; i < skinIds.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(skinIds[i]);
            }

            return builder.ToString();
        }
    }
}
