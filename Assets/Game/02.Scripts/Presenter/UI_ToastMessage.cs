using System.Collections;
using TMPro;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UI_ToastMessage : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private float _fadeInDuration = 0.12f;
        [SerializeField] private float _visibleDuration = 1f;
        [SerializeField] private float _fadeOutDuration = 0.18f;

        private Coroutine _showRoutine;

        private void Awake()
        {
            HideImmediate();
        }

        private void OnDisable()
        {
            StopShowRoutine();
            ApplyHiddenState();
        }

        public void Show(string message)
        {
            _messageText.text = message;
            StopShowRoutine();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _showRoutine = StartCoroutine(ShowRoutine());
        }

        public void HideImmediate()
        {
            StopShowRoutine();
            ApplyHiddenState();
        }

        private IEnumerator ShowRoutine()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            yield return FadeAlpha(0f, 1f, _fadeInDuration);

            var elapsed = 0f;
            var duration = Mathf.Max(0f, _visibleDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return FadeAlpha(1f, 0f, _fadeOutDuration);

            _showRoutine = null;
            ApplyHiddenState();
        }

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            var safeDuration = Mathf.Max(0.01f, duration);
            var elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        private void StopShowRoutine()
        {
            if (_showRoutine == null)
            {
                return;
            }

            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        private void ApplyHiddenState()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
