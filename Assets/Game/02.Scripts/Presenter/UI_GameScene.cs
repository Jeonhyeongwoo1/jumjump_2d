using TMPro;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UI_GameScene : BaseSceneUI
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _goldText;

        public void SetScore(int score) => _scoreText.text = score.ToString();
        public void SetGold(int gold) => _goldText.text = gold.ToString();
    }
}
