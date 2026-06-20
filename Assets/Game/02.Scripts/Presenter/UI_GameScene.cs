using System;
using System.Collections;
using System.Collections.Generic;
using JumJump.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumJump.Presenter
{
    public sealed class UI_GameScene : BaseSceneUI
    {
        [SerializeField] private GameObject _scorePanel;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private GameObject _startCountdownPanel;
        [SerializeField] private TMP_Text _startCountdownText;
        [SerializeField] private Button _gameReadyButton;
        [SerializeField] private Button _selectCharacterButton;
        [SerializeField] private Button _selectCharacterHitAreaButton;
        [SerializeField] private TMP_Text _gameReadyPromptText;
        [SerializeField] private GameObject _characterSelectPanel;
        [SerializeField] private Button _characterConfirmButton;
        [SerializeField] private ScrollRect _characterScrollRect;
        [SerializeField] private RectTransform _characterPageRoot;
        [SerializeField] private UI_CharacterPage _characterPageTemplate;
        [SerializeField] private float _gameReadyPromptPulseDuration = 0.9f;
        [SerializeField] private float _gameReadyPromptPulseScale = 1.1f;
        [SerializeField] private float _gameReadyPromptLift = 18f;
        [SerializeField] private float _gameReadyPromptMinAlpha = 0.68f;
        [SerializeField] private float _scorePopDuration = 0.18f;
        [SerializeField] private float _scoreSettleDuration = 0.12f;
        [SerializeField] private float _scorePopScale = 1.18f;
        [SerializeField] private Color _scoreFlashColor = new Color(1f, 0.82f, 0.25f, 1f);

        private Coroutine _scoreAnimationRoutine;
        private Coroutine _gameReadyPromptAnimationRoutine;
        private Vector3 _scoreTextDefaultScale;
        private Color _scoreTextDefaultColor;
        private Vector3 _gameReadyPromptDefaultScale;
        private Vector2 _gameReadyPromptDefaultAnchoredPosition;
        private Color _gameReadyPromptDefaultColor;
        private Action _onGameReadyClicked;
        private Action<int> _onCharacterSelected;
        private readonly List<UI_CharacterPage> _characterPages = new List<UI_CharacterPage>(4);
        private int _selectedCharacterSkinId;

        protected override void Awake()
        {
            base.Awake();
            PrepareCharacterPageTemplate();
            _scoreTextDefaultScale = _scoreText.rectTransform.localScale;
            _scoreTextDefaultColor = _scoreText.color;
            _gameReadyPromptDefaultScale = _gameReadyPromptText.rectTransform.localScale;
            _gameReadyPromptDefaultAnchoredPosition = _gameReadyPromptText.rectTransform.anchoredPosition;
            _gameReadyPromptDefaultColor = _gameReadyPromptText.color;
            ShowReady();
        }

        public void AddEvents(Action onGameReadyClicked, Action<int> onCharacterSelected)
        {
            _onGameReadyClicked = onGameReadyClicked;
            _onCharacterSelected = onCharacterSelected;
            ButtonUtils.SetListener(_gameReadyButton, OnGameReadyClicked);
            ButtonUtils.SetListener(_selectCharacterButton, ShowCharacterSelectPanel);
            ButtonUtils.SetListener(_selectCharacterHitAreaButton, ShowCharacterSelectPanel);
            ButtonUtils.SetListener(_characterConfirmButton, ConfirmSelectedCharacter);
        }

        public void RemoveEvents()
        {
            _onGameReadyClicked = null;
            _onCharacterSelected = null;
            _gameReadyButton.onClick.RemoveAllListeners();
            _selectCharacterButton.onClick.RemoveAllListeners();
            _selectCharacterHitAreaButton.onClick.RemoveAllListeners();
            _characterConfirmButton.onClick.RemoveAllListeners();
            for (var i = 0; i < _characterPages.Count; i++)
            {
                _characterPages[i].RemoveEvents();
            }
        }

        public void ShowReady()
        {
            HideScorePanel();
            HideStartCountdown();
            ShowGameReadyPanel();
            HideCharacterSelectPanel();
        }

        public void SetScore(int score) => _scoreText.text = score.ToString();
        public void SetScoreAnimated(int score)
        {
            SetScore(score);
            RestartScoreAnimation();
        }

        public void SetGold(int gold) => _goldText.text = gold.ToString();

        public void ShowScorePanel()
        {
            _scorePanel.SetActive(true);
        }

        public void HideScorePanel()
        {
            _scorePanel.SetActive(false);
        }

        public void ShowGameReadyPanel()
        {
            _gameReadyButton.gameObject.SetActive(true);
            StartGameReadyPromptAnimation();
        }

        public void HideGameReadyPanel()
        {
            StopGameReadyPromptAnimation();
            _gameReadyButton.gameObject.SetActive(false);
            HideCharacterSelectPanel();
        }

        public void SetSelectedCharacter(int skinId)
        {
            _selectedCharacterSkinId = skinId;
        }

        public void SetCharacterPages(IReadOnlyList<int> skinIds, IReadOnlyList<Sprite> sprites)
        {
            ClearGeneratedCharacterPages();

            var pageCount = Mathf.Min(skinIds.Count, sprites.Count);
            for (var i = 0; i < pageCount; i++)
            {
                var page = Instantiate(_characterPageTemplate, _characterPageRoot);
                page.gameObject.name = $"CharacterPage_{skinIds[i]}";
                page.Initialize(skinIds[i], sprites[i], OnCharacterPageClicked);
                page.gameObject.SetActive(true);
                _characterPages.Add(page);
            }

            RefreshCharacterScrollLayout();
            MoveScrollToSelectedCharacter();
        }

        private void HideCharacterSelectPanel()
        {
            _characterSelectPanel.SetActive(false);
        }

        private void ShowCharacterSelectPanel()
        {
            _characterSelectPanel.SetActive(true);
            MoveScrollToSelectedCharacter();
        }

        private void ConfirmSelectedCharacter()
        {
            if (_characterPages.Count <= 0)
            {
                return;
            }

            var skinId = _characterPages[ResolveCurrentCharacterIndex()].SkinId;
            _onCharacterSelected.Invoke(skinId);
            HideCharacterSelectPanel();
        }

        private void MoveScrollToSelectedCharacter()
        {
            var maxIndex = _characterPages.Count - 1;
            if (maxIndex <= 0)
            {
                _characterScrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            var selectedIndex = ResolveSelectedCharacterIndex();
            _characterScrollRect.horizontalNormalizedPosition = (float)selectedIndex / maxIndex;
            _characterScrollRect.SendMessage("ChangePage", selectedIndex, SendMessageOptions.DontRequireReceiver);
        }

        private int ResolveCurrentCharacterIndex()
        {
            var maxIndex = _characterPages.Count - 1;
            if (maxIndex <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(
                Mathf.RoundToInt(_characterScrollRect.horizontalNormalizedPosition * maxIndex),
                0,
                maxIndex);
        }

        private int ResolveSelectedCharacterIndex()
        {
            for (var i = 0; i < _characterPages.Count; i++)
            {
                if (_characterPages[i].SkinId == _selectedCharacterSkinId)
                {
                    return i;
                }
            }

            return 0;
        }

        private void OnCharacterPageClicked(int skinId)
        {
            _selectedCharacterSkinId = skinId;
            MoveScrollToSelectedCharacter();
        }

        private void PrepareCharacterPageTemplate()
        {
            var templateTransform = _characterPageTemplate.transform;
            templateTransform.SetParent(_characterSelectPanel.transform, false);
            _characterPageTemplate.gameObject.SetActive(false);

            for (var i = _characterPageRoot.childCount - 1; i >= 0; i--)
            {
                var child = _characterPageRoot.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void ClearGeneratedCharacterPages()
        {
            for (var i = 0; i < _characterPages.Count; i++)
            {
                var page = _characterPages[i];
                page.RemoveEvents();
                page.transform.SetParent(null, false);
                Destroy(page.gameObject);
            }

            _characterPages.Clear();
        }

        private void RefreshCharacterScrollLayout()
        {
            _characterScrollRect.SendMessage("UpdateLayout", false, SendMessageOptions.DontRequireReceiver);
        }

        private void OnGameReadyClicked()
        {
            if (_characterSelectPanel.activeSelf)
            {
                return;
            }

            _onGameReadyClicked.Invoke();
        }

        public void ShowStartCountdown(int seconds)
        {
            _startCountdownPanel.SetActive(true);
            SetStartCountdown(seconds);
            SetStartCountdownProgress(0f);
        }

        public void SetStartCountdown(int seconds)
        {
            _startCountdownText.text = seconds.ToString();
        }

        public void SetStartCountdownProgress(float normalized)
        {
            var progress = Mathf.Clamp01(normalized);
            var scale = ResolveStartCountdownScale(progress);
            _startCountdownText.rectTransform.localScale = Vector3.one * scale;
            _startCountdownText.alpha = ResolveStartCountdownAlpha(progress);
        }

        public void HideStartCountdown()
        {
            _startCountdownText.rectTransform.localScale = Vector3.one;
            _startCountdownText.alpha = 1f;
            _startCountdownPanel.SetActive(false);
        }

        private void RestartScoreAnimation()
        {
            if (_scoreAnimationRoutine != null)
            {
                StopCoroutine(_scoreAnimationRoutine);
            }

            _scoreAnimationRoutine = StartCoroutine(ScoreAnimationRoutine());
        }

        private IEnumerator ScoreAnimationRoutine()
        {
            var popDuration = Mathf.Max(0.01f, _scorePopDuration);
            var settleDuration = Mathf.Max(0.01f, _scoreSettleDuration);
            var elapsed = 0f;
            var popScale = _scoreTextDefaultScale * Mathf.Max(1f, _scorePopScale);

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / popDuration);
                var eased = Mathf.SmoothStep(0f, 1f, normalized);
                _scoreText.rectTransform.localScale = Vector3.Lerp(_scoreTextDefaultScale, popScale, eased);
                _scoreText.color = Color.Lerp(_scoreTextDefaultColor, _scoreFlashColor, eased);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / settleDuration);
                var eased = Mathf.SmoothStep(0f, 1f, normalized);
                _scoreText.rectTransform.localScale = Vector3.Lerp(popScale, _scoreTextDefaultScale, eased);
                _scoreText.color = Color.Lerp(_scoreFlashColor, _scoreTextDefaultColor, eased);
                yield return null;
            }

            ResetScoreTextAnimation();
            _scoreAnimationRoutine = null;
        }

        private void ResetScoreTextAnimation()
        {
            _scoreText.rectTransform.localScale = _scoreTextDefaultScale;
            _scoreText.color = _scoreTextDefaultColor;
        }

        private void StartGameReadyPromptAnimation()
        {
            if (_gameReadyPromptAnimationRoutine != null)
            {
                return;
            }

            ResetGameReadyPromptAnimation();
            _gameReadyPromptAnimationRoutine = StartCoroutine(GameReadyPromptAnimationRoutine());
        }

        private void StopGameReadyPromptAnimation()
        {
            if (_gameReadyPromptAnimationRoutine != null)
            {
                StopCoroutine(_gameReadyPromptAnimationRoutine);
                _gameReadyPromptAnimationRoutine = null;
            }

            ResetGameReadyPromptAnimation();
        }

        private IEnumerator GameReadyPromptAnimationRoutine()
        {
            var duration = Mathf.Max(0.01f, _gameReadyPromptPulseDuration);
            var pulseScale = Mathf.Max(1f, _gameReadyPromptPulseScale);
            var minAlpha = Mathf.Clamp01(_gameReadyPromptMinAlpha);
            var elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                var pingPong = Mathf.PingPong(elapsed / duration, 1f);
                var eased = Mathf.SmoothStep(0f, 1f, pingPong);
                _gameReadyPromptText.rectTransform.localScale = Vector3.Lerp(
                    _gameReadyPromptDefaultScale,
                    _gameReadyPromptDefaultScale * pulseScale,
                    eased);
                _gameReadyPromptText.rectTransform.anchoredPosition = _gameReadyPromptDefaultAnchoredPosition +
                        Vector2.up * (_gameReadyPromptLift * eased);

                var color = _gameReadyPromptDefaultColor;
                color.a = Mathf.Lerp(minAlpha, _gameReadyPromptDefaultColor.a, eased);
                _gameReadyPromptText.color = color;
                yield return null;
            }
        }

        private void ResetGameReadyPromptAnimation()
        {
            _gameReadyPromptText.rectTransform.localScale = _gameReadyPromptDefaultScale;
            _gameReadyPromptText.rectTransform.anchoredPosition = _gameReadyPromptDefaultAnchoredPosition;
            _gameReadyPromptText.color = _gameReadyPromptDefaultColor;
        }

        private float ResolveStartCountdownScale(float progress)
        {
            if (progress < GameConst.UI.StartCountdownPopInDuration)
            {
                var popT = progress / GameConst.UI.StartCountdownPopInDuration;
                return Mathf.Lerp(
                    GameConst.UI.StartCountdownScaleFrom,
                    GameConst.UI.StartCountdownScalePop,
                    Mathf.SmoothStep(0f, 1f, popT));
            }

            if (progress < GameConst.UI.StartCountdownSettleDuration)
            {
                var settleT = (progress - GameConst.UI.StartCountdownPopInDuration) /
                        (GameConst.UI.StartCountdownSettleDuration - GameConst.UI.StartCountdownPopInDuration);
                return Mathf.Lerp(
                    GameConst.UI.StartCountdownScalePop,
                    GameConst.UI.StartCountdownScaleSettle,
                    Mathf.SmoothStep(0f, 1f, settleT));
            }

            if (progress < GameConst.UI.StartCountdownFadeOutStart)
            {
                return GameConst.UI.StartCountdownScaleSettle;
            }

            var fadeT = (progress - GameConst.UI.StartCountdownFadeOutStart) /
                        (1f - GameConst.UI.StartCountdownFadeOutStart);
            return Mathf.Lerp(
                GameConst.UI.StartCountdownScaleSettle,
                GameConst.UI.StartCountdownScaleOut,
                Mathf.SmoothStep(0f, 1f, fadeT));
        }

        private float ResolveStartCountdownAlpha(float progress)
        {
            if (progress < GameConst.UI.StartCountdownPopInDuration)
            {
                var popT = progress / GameConst.UI.StartCountdownPopInDuration;
                return Mathf.SmoothStep(0f, 1f, popT);
            }

            if (progress < GameConst.UI.StartCountdownFadeOutStart)
            {
                return 1f;
            }

            var fadeT = (progress - GameConst.UI.StartCountdownFadeOutStart) /
                    (1f - GameConst.UI.StartCountdownFadeOutStart);
            return Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, fadeT));
        }

        private void OnDisable()
        {
            if (_scoreAnimationRoutine != null)
            {
                StopCoroutine(_scoreAnimationRoutine);
                _scoreAnimationRoutine = null;
            }

            ResetScoreTextAnimation();
            StopGameReadyPromptAnimation();
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }
    }
}
