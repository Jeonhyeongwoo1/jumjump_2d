namespace JumJump.Model
{
    public sealed class PlayerModel
    {
        private const int DefaultSelectedPlayerSkinId = (int)PlayerSkinType.Player_1;

        public int HighScore { get; private set; }
        public int Gold { get; private set; }
        public int SelectedPlayerSkinId { get; private set; } = DefaultSelectedPlayerSkinId;

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

        public bool AddGold(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            Gold += amount;
            return true;
        }
    }
}
