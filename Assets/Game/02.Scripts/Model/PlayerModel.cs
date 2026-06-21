using System.Collections.Generic;

namespace JumJump.Model
{
    public sealed class PlayerModel
    {
        private const int DefaultSelectedPlayerSkinId = (int)PlayerSkinType.Player_1;

        public int HighScore { get; private set; }
        public int Gold { get; private set; }
        public int SelectedPlayerSkinId { get; private set; } = DefaultSelectedPlayerSkinId;
        public IEnumerable<int> OwnedPlayerSkinIds => _ownedPlayerSkinIds;

        private readonly HashSet<int> _ownedPlayerSkinIds = new HashSet<int>();

        public PlayerModel()
        {
            UnlockPlayerSkin(DefaultSelectedPlayerSkinId);
        }

        public void SetHighScore(int highScore)
        {
            HighScore = highScore < 0 ? 0 : highScore;
        }

        public bool TryUpdateHighScore(int score)
        {
            if (score <= HighScore)
            {
                return false;
            }

            HighScore = score;
            return true;
        }

        public void SetGold(int gold)
        {
            Gold = gold < 0 ? 0 : gold;
        }

        public void SetSelectedPlayerSkinId(int skinId)
        {
            SelectedPlayerSkinId = skinId <= 0 ? DefaultSelectedPlayerSkinId : skinId;
        }

        public void ValidateSelectedPlayerSkinOwnership()
        {
            if (!_ownedPlayerSkinIds.Contains(SelectedPlayerSkinId))
            {
                SelectedPlayerSkinId = DefaultSelectedPlayerSkinId;
            }
        }

        public void SetOwnedPlayerSkinIds(IEnumerable<int> skinIds)
        {
            _ownedPlayerSkinIds.Clear();
            UnlockPlayerSkin(DefaultSelectedPlayerSkinId);

            foreach (var skinId in skinIds)
            {
                UnlockPlayerSkin(skinId);
            }

            ValidateSelectedPlayerSkinOwnership();
        }

        public bool OwnsPlayerSkin(int skinId)
        {
            return _ownedPlayerSkinIds.Contains(skinId);
        }

        public bool TryPurchasePlayerSkin(int skinId, int price)
        {
            if (skinId <= 0)
            {
                return false;
            }

            if (OwnsPlayerSkin(skinId))
            {
                return true;
            }

            var normalizedPrice = price < 0 ? 0 : price;
            if (Gold < normalizedPrice)
            {
                return false;
            }

            Gold -= normalizedPrice;
            UnlockPlayerSkin(skinId);
            return true;
        }

        public bool AddGold(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            Gold += amount;
            return true;
        }

        private void UnlockPlayerSkin(int skinId)
        {
            if (skinId > 0)
            {
                _ownedPlayerSkinIds.Add(skinId);
            }
        }
    }
}
