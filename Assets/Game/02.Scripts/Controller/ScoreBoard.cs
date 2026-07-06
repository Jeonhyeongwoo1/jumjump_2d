using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class ScoreBoard : MonoBehaviour
    {
        private const int ScoreTextSortingOrderOffset = 1;
        private const string BestScoreText = "BEST!";

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text[] _scoreTexts;
        [SerializeField] private float _bestScorePopDuration = 0.18f;
        [SerializeField] private float _bestScoreHoldDuration = 0.28f;
        [SerializeField] private float _bestScoreMessageDuration = 0.42f;
        [SerializeField] private float _bestScoreFadeDuration = 0.18f;
        [SerializeField] private float _bestScorePopScale = 1.22f;
        [SerializeField] private Color _bestScoreColor = new Color(1f, 0.88f, 0.2f, 1f);

        private TMP_Text _activeScoreText;
        private Coroutine _bestScoreRoutine;
        private Vector3 _defaultScale;
        private Color _spriteDefaultColor;
        private Color _activeScoreTextDefaultColor;

        private void Awake()
        {
            _defaultScale = transform.localScale;
            _spriteDefaultColor = _spriteRenderer.color;
        }

        public void Show(Vector3 worldPosition, Sprite sprite, int sortingOrder, int spriteIndex, int highScore)
        {
            StopBestScoreRoutine();
            transform.position = worldPosition;
            transform.localScale = _defaultScale;
            _spriteRenderer.color = _spriteDefaultColor;
            ApplyVisual(sprite, sortingOrder, spriteIndex, highScore);
            gameObject.SetActive(true);
        }

        public void UpdateVisual(Sprite sprite, int sortingOrder, int spriteIndex, int highScore)
        {
            ApplyVisual(sprite, sortingOrder, spriteIndex, highScore);
        }

        public void PlayBestScoreReached(Action onComplete)
        {
            StopBestScoreRoutine();
            _bestScoreRoutine = StartCoroutine(BestScoreReachedRoutine(onComplete));
        }

        public void Hide()
        {
            StopBestScoreRoutine();
            transform.localScale = _defaultScale;
            _spriteRenderer.color = _spriteDefaultColor;
            ResetActiveScoreTextVisual();
            gameObject.SetActive(false);
        }

        private void ApplyVisual(Sprite sprite, int sortingOrder, int spriteIndex, int highScore)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sortingOrder = sortingOrder;
            UpdateScoreText(spriteIndex, highScore, sortingOrder);
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
                    _activeScoreText = scoreText;
                    _activeScoreTextDefaultColor = scoreText.color;
                    ResetActiveScoreTextVisual();
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

        private IEnumerator BestScoreReachedRoutine(Action onComplete)
        {
            var popDuration = Mathf.Max(0.01f, _bestScorePopDuration);
            var holdDuration = Mathf.Max(0f, _bestScoreHoldDuration);
            var messageDuration = Mathf.Max(0.01f, _bestScoreMessageDuration);
            var fadeDuration = Mathf.Max(0.01f, _bestScoreFadeDuration);
            var popScale = _defaultScale * Mathf.Max(1f, _bestScorePopScale);

            yield return AnimatePop(popDuration, _defaultScale, popScale);

            if (_activeScoreText != null)
            {
                _activeScoreText.color = _bestScoreColor;
            }

            if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            if (_activeScoreText != null)
            {
                _activeScoreText.SetText(BestScoreText);
            }

            yield return new WaitForSeconds(messageDuration);
            yield return AnimateFadeOut(fadeDuration);

            _bestScoreRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator AnimatePop(float duration, Vector3 fromScale, Vector3 popScale)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var pingPong = normalized < 0.5f
                    ? normalized * 2f
                    : 1f - (normalized - 0.5f) * 2f;
                var eased = Mathf.SmoothStep(0f, 1f, pingPong);
                transform.localScale = Vector3.Lerp(fromScale, popScale, eased);
                if (_activeScoreText != null)
                {
                    _activeScoreText.color = Color.Lerp(_activeScoreTextDefaultColor, _bestScoreColor, eased);
                }

                yield return null;
            }

            transform.localScale = fromScale;
        }

        private IEnumerator AnimateFadeOut(float duration)
        {
            var elapsed = 0f;
            var spriteColor = _spriteRenderer.color;
            var scoreTextColor = _activeScoreText == null ? Color.white : _activeScoreText.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, normalized);
                var nextSpriteColor = spriteColor;
                nextSpriteColor.a = Mathf.Lerp(spriteColor.a, 0f, eased);
                _spriteRenderer.color = nextSpriteColor;

                if (_activeScoreText != null)
                {
                    var nextScoreTextColor = scoreTextColor;
                    nextScoreTextColor.a = Mathf.Lerp(scoreTextColor.a, 0f, eased);
                    _activeScoreText.color = nextScoreTextColor;
                }

                yield return null;
            }
        }

        private void StopBestScoreRoutine()
        {
            if (_bestScoreRoutine == null)
            {
                return;
            }

            StopCoroutine(_bestScoreRoutine);
            _bestScoreRoutine = null;
        }

        private void ResetActiveScoreTextVisual()
        {
            if (_activeScoreText == null)
            {
                return;
            }

            _activeScoreText.color = _activeScoreTextDefaultColor;
        }
    }
}
