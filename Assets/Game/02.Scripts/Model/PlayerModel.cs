namespace JumJump.Model
{
    public sealed class PlayerModel
    {
        public int HighScore { get; private set; }
        public int Gold { get; private set; }

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
