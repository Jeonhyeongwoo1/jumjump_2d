using TMPro;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class ScoreBoard : MonoBehaviour
    {
        private const int ScoreTextSortingOrderOffset = 1;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text[] _scoreTexts;

        public void Show(Vector3 worldPosition, Sprite sprite, int sortingOrder, int spriteIndex, int highScore)
        {
            transform.position = worldPosition;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sortingOrder = sortingOrder;
            UpdateScoreText(spriteIndex, highScore, sortingOrder);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateScoreText(int spriteIndex, int highScore, int sortingOrder)
        {
            var text = highScore.ToString();
            var activeIndex = Mathf.Clamp(spriteIndex, 0, _scoreTexts.Length - 1);
            for (var i = 0; i < _scoreTexts.Length; i++)
            {
                var scoreText = _scoreTexts[i];
                ApplyScoreTextSorting(scoreText, sortingOrder);
                var isActive = i == activeIndex;
                scoreText.gameObject.SetActive(isActive);
                if (isActive)
                {
                    scoreText.SetText(text);
                }
            }
        }

        private void ApplyScoreTextSorting(TMP_Text scoreText, int sortingOrder)
        {
            if (scoreText is TextMeshPro textMeshPro)
            {
                textMeshPro.sortingLayerID = _spriteRenderer.sortingLayerID;
                textMeshPro.sortingOrder = sortingOrder + ScoreTextSortingOrderOffset;
            }
        }
    }
}
