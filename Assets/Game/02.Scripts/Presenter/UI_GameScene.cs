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
        [SerializeField] private Button _characterBuyButton;
        [SerializeField] private TMP_Text _characterNameText;
        [SerializeField] private TMP_Text _characterPriceText;
        [SerializeField] private TMP_Text _characterConfirmButtonText;
        [SerializeField] private ScrollRect _characterScrollRect;
        [SerializeField] private RectTransform _characterPageRoot;
        [SerializeField] private UI_CharacterPage _characterPageTemplate;
        [SerializeField] private UI_ToastMessage _toastMessagePrefab;
        [SerializeField] private string _notEnoughGoldMessage = "Not enough gold";
        [SerializeField] private float _gameReadyPromptPulseDuration = 0.9f;
        [SerializeField] private float _gameReadyPromptPulseScale = 1.1f;
        [SerializeField] private float _gameReadyPromptLift = 18f;
        [SerializeField] private float _gameReadyPromptMinAlpha = 0.68f;
        [SerializeField] private float _scorePopDuration = 0.18f;
        [SerializeField] private float _scoreSettleDuration = 0.12f;
        [SerializeField] private float _scorePopScale = 1.18f;
        [SerializeField] private float _bestScorePopScale = 1.35f;
        [SerializeField] private Color _scoreFlashColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _bestScoreFlashColor = new Color(1f, 0.94f, 0.18f, 1f);

        private Coroutine _scoreAnimationRoutine;
        private Coroutine _gameReadyPromptAnimationRoutine;
        private Vector3 _scoreTextDefaultScale;
        private Color _scoreTextDefaultColor;
        private Vector3 _gameReadyPromptDefaultScale;
        private Vector2 _gameReadyPromptDefaultAnchoredPosition;
        private Color _gameReadyPromptDefaultColor;
        private Action _onGameReadyClicked;
        private Func<int, bool> _onCharacterConfirmed;
        private Func<int, bool> _onCharacterPurchased;
        private readonly List<UI_CharacterPage> _characterPages = new List<UI_CharacterPage>(8);
        private readonly List<int> _characterPrices = new List<int>(8);
        private readonly List<bool> _characterOwned = new List<bool>(8);
        private readonly List<string> _characterDisplayNames = new List<string>(8);
        private UI_ToastMessage _toastMessage;
        private int _selectedCharacterSkinId;
        private int _currentGold;

        protected override void Awake()
        {
            base.Awake();
            PrepareCharacterPageTemplate();
            _toastMessage = Instantiate(_toastMessagePrefab, transform, false);
            _toastMessage.HideImmediate();
            _scoreTextDefaultScale = _scoreText.rectTransform.localScale;
            _scoreTextDefaultColor = _scoreText.color;
            _gameReadyPromptDefaultScale = _gameReadyPromptText.rectTransform.localScale;
            _gameReadyPromptDefaultAnchoredPosition = _gameReadyPromptText.rectTransform.anchoredPosition;
            _gameReadyPromptDefaultColor = _gameReadyPromptText.color;
            ShowReady();
        }

        public void AddEvents(
            Action onGameReadyClicked,
            Func<int, bool> onCharacterConfirmed,
            Func<int, bool> onCharacterPurchased)
        {
            _onGameReadyClicked = onGameReadyClicked;
            _onCharacterConfirmed = onCharacterConfirmed;
            _onCharacterPurchased = onCharacterPurchased;
            ButtonUtils.SetListener(_gameReadyButton, OnGameReadyClicked);
            ButtonUtils.SetListener(_selectCharacterButton, ShowCharacterSelectPanel);
            ButtonUtils.SetListener(_selectCharacterHitAreaButton, ShowCharacterSelectPanel);
            ButtonUtils.SetListener(_characterConfirmButton, ConfirmSelectedCharacter);
            ButtonUtils.SetListener(_characterBuyButton, BuySelectedCharacter);
            _characterScrollRect.onValueChanged.AddListener(OnCharacterScrollValueChanged);
        }

        public void RemoveEvents()
        {
            _onGameReadyClicked = null;
            _onCharacterConfirmed = null;
            _onCharacterPurchased = null;
            _gameReadyButton.onClick.RemoveAllListeners();
            _selectCharacterButton.onClick.RemoveAllListeners();
            _selectCharacterHitAreaButton.onClick.RemoveAllListeners();
            _characterConfirmButton.onClick.RemoveAllListeners();
            _characterBuyButton.onClick.RemoveAllListeners();
            _characterScrollRect.onValueChanged.RemoveListener(OnCharacterScrollValueChanged);
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
            RestartScoreAnimation(_scorePopScale, _scoreFlashColor);
        }

        public void SetScoreBestScoreAnimated(int score)
        {
            SetScore(score);
            RestartScoreAnimation(_bestScorePopScale, _bestScoreFlashColor);
        }

        public void SetGold(int gold)
        {
            _currentGold = gold;
            _goldText.text = gold.ToString();
            RefreshCharacterPurchaseState();
        }

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
            RefreshCharacterPurchaseState();
        }

        public void SetCharacterPages(
            IReadOnlyList<int> skinIds,
            IReadOnlyList<Sprite> sprites,
            IReadOnlyList<int> prices,
            IReadOnlyList<bool> owned,
            IReadOnlyList<string> displayNames)
        {
            ClearGeneratedCharacterPages();
            _characterPrices.Clear();
            _characterOwned.Clear();
            _characterDisplayNames.Clear();

            var pageCount = skinIds.Count;
            pageCount = Mathf.Min(pageCount, sprites.Count);
            pageCount = Mathf.Min(pageCount, prices.Count);
            pageCount = Mathf.Min(pageCount, owned.Count);
            pageCount = Mathf.Min(pageCount, displayNames.Count);
            for (var i = 0; i < pageCount; i++)
            {
                var page = Instantiate(_characterPageTemplate, _characterPageRoot);
                page.gameObject.name = $"CharacterPage_{skinIds[i]}";
                page.Initialize(skinIds[i], sprites[i], OnCharacterPageClicked);
                page.gameObject.SetActive(true);
                _characterPages.Add(page);
                _characterPrices.Add(prices[i]);
                _characterOwned.Add(owned[i]);
                _characterDisplayNames.Add(displayNames[i]);
            }

            RefreshCharacterScrollLayout();
            MoveScrollToSelectedCharacter();
            RefreshCharacterPurchaseState();
        }

        public void SetCharacterOwned(int skinId, bool owned)
        {
            var index = ResolveCharacterIndex(skinId);
            if (index < 0)
            {
                return;
            }

            _characterOwned[index] = owned;
            RefreshCharacterPurchaseState();
        }

        private void HideCharacterSelectPanel()
        {
            _characterSelectPanel.SetActive(false);
        }

        private void ShowCharacterSelectPanel()
        {
            _characterSelectPanel.SetActive(true);
            MoveScrollToSelectedCharacter();
            RefreshCharacterPurchaseState();
        }

        private void ConfirmSelectedCharacter()
        {
            if (_characterPages.Count <= 0)
            {
                return;
            }

            var currentIndex = ResolveCurrentCharacterIndex();
            if (!_characterOwned[currentIndex])
            {
                RefreshCharacterPurchaseState();
                return;
            }

            var skinId = _characterPages[currentIndex].SkinId;
            if (_onCharacterConfirmed != null && _onCharacterConfirmed.Invoke(skinId))
            {
                HideCharacterSelectPanel();
                return;
            }

            RefreshCharacterPurchaseState();
        }

        private void BuySelectedCharacter()
        {
            if (_characterPages.Count <= 0)
            {
                return;
            }

            var currentIndex = ResolveCurrentCharacterIndex();
            if (_characterOwned[currentIndex])
            {
                RefreshCharacterPurchaseState();
                return;
            }

            if (IsCharacterPurchaseBlockedByGold(currentIndex))
            {
                ShowNotEnoughGoldToast();
                RefreshCharacterPurchaseState();
                return;
            }

            var skinId = _characterPages[currentIndex].SkinId;
            if (_onCharacterPurchased != null && _onCharacterPurchased.Invoke(skinId))
            {
                RefreshCharacterPurchaseState();
                return;
            }

            ShowNotEnoughGoldToast();
            RefreshCharacterPurchaseState();
        }

        private void MoveScrollToSelectedCharacter()
        {
            MoveScrollToCharacterIndex(ResolveSelectedCharacterIndex());
        }

        private void MoveScrollToCharacterIndex(int index)
        {
            var maxIndex = _characterPages.Count - 1;
            if (maxIndex <= 0)
            {
                _characterScrollRect.horizontalNormalizedPosition = 0f;
                RefreshCharacterPurchaseState();
                return;
            }

            var clampedIndex = Mathf.Clamp(index, 0, maxIndex);
            _characterScrollRect.horizontalNormalizedPosition = (float)clampedIndex / maxIndex;
            _characterScrollRect.SendMessage("ChangePage", clampedIndex, SendMessageOptions.DontRequireReceiver);
            RefreshCharacterPurchaseState();
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
            return ResolveCharacterIndex(_selectedCharacterSkinId);
        }

        private int ResolveCharacterIndex(int skinId)
        {
            for (var i = 0; i < _characterPages.Count; i++)
            {
                if (_characterPages[i].SkinId == skinId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnCharacterPageClicked(int skinId)
        {
            var index = ResolveCharacterIndex(skinId);
            if (index < 0)
            {
                return;
            }

            MoveScrollToCharacterIndex(index);
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
            _characterPrices.Clear();
            _characterOwned.Clear();
            _characterDisplayNames.Clear();
            RefreshCharacterPurchaseState();
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

        private void OnCharacterScrollValueChanged(Vector2 normalizedPosition)
        {
            RefreshCharacterPurchaseState();
        }

        private void RefreshCharacterPurchaseState()
        {
            if (_characterPages.Count <= 0)
            {
                _characterNameText.text = string.Empty;
                _characterPriceText.text = string.Empty;
                _characterConfirmButtonText.text = string.Empty;
                _characterConfirmButton.interactable = false;
                _characterConfirmButton.gameObject.SetActive(false);
                _characterBuyButton.interactable = false;
                _characterBuyButton.gameObject.SetActive(false);
                return;
            }

            var currentIndex = ResolveCurrentCharacterIndex();
            var isOwned = _characterOwned[currentIndex];
            var price = _characterPrices[currentIndex];
            var isSelected = _characterPages[currentIndex].SkinId == _selectedCharacterSkinId;
            var canSelect = isOwned && !isSelected;

            _characterNameText.text = _characterDisplayNames[currentIndex];
            _characterPriceText.text = ResolveCharacterPriceText(price, isOwned);
            _characterConfirmButtonText.text = canSelect ? "Select" : string.Empty;
            _characterConfirmButton.gameObject.SetActive(canSelect);
            _characterConfirmButton.interactable = canSelect;
            _characterBuyButton.gameObject.SetActive(!isOwned);
            _characterBuyButton.interactable = true;
        }

        private bool IsCharacterPurchaseBlockedByGold(int characterIndex)
        {
            return !_characterOwned[characterIndex] && _characterPrices[characterIndex] > _currentGold;
        }

        private void ShowNotEnoughGoldToast()
        {
            _toastMessage.Show(_notEnoughGoldMessage);
        }

        private string ResolveCharacterPriceText(int price, bool isOwned)
        {
            if (isOwned)
            {
                return "Owned";
            }

            if (price <= 0)
            {
                return "Free";
            }

            return price.ToString();
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

        private void RestartScoreAnimation(float popScaleMultiplier, Color flashColor)
        {
            if (_scoreAnimationRoutine != null)
            {
                StopCoroutine(_scoreAnimationRoutine);
            }

            _scoreAnimationRoutine = StartCoroutine(ScoreAnimationRoutine(popScaleMultiplier, flashColor));
        }

        private IEnumerator ScoreAnimationRoutine(float popScaleMultiplier, Color flashColor)
        {
            var popDuration = Mathf.Max(0.01f, _scorePopDuration);
            var settleDuration = Mathf.Max(0.01f, _scoreSettleDuration);
            var elapsed = 0f;
            var popScale = _scoreTextDefaultScale * Mathf.Max(1f, popScaleMultiplier);

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / popDuration);
                var eased = Mathf.SmoothStep(0f, 1f, normalized);
                _scoreText.rectTransform.localScale = Vector3.Lerp(_scoreTextDefaultScale, popScale, eased);
                _scoreText.color = Color.Lerp(_scoreTextDefaultColor, flashColor, eased);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / settleDuration);
                var eased = Mathf.SmoothStep(0f, 1f, normalized);
                _scoreText.rectTransform.localScale = Vector3.Lerp(popScale, _scoreTextDefaultScale, eased);
                _scoreText.color = Color.Lerp(flashColor, _scoreTextDefaultColor, eased);
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
