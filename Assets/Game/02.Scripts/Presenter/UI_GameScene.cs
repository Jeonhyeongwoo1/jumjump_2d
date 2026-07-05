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
        [SerializeField] private Button _adRewardButton;
        [SerializeField] private TMP_Text _adRewardGoldText;
        [SerializeField] private TMP_Text _gameReadyPromptText;
        [SerializeField] private GameObject _characterSelectPanel;
        [SerializeField] private Button _characterConfirmButton;
        [SerializeField] private Button _characterBuyButton;
        [SerializeField] private TMP_Text _characterNameText;
        [SerializeField] private TMP_Text _characterDescriptionText;
        [SerializeField] private TMP_Text _characterPriceText;
        [SerializeField] private TMP_Text _characterConfirmButtonText;
        [SerializeField] private ScrollRect _characterScrollRect;
        [SerializeField] private Button _characterPrevButton;
        [SerializeField] private Button _characterNextButton;
        [SerializeField] private RectTransform _characterPageRoot;
        [SerializeField] private UI_CharacterPage _characterPageTemplate;
        [SerializeField] private UI_ToastMessage _toastMessagePrefab;
        [SerializeField] private UI_CharacterUnlockFX _characterUnlockFXPrefab;
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
        [SerializeField] private UI_GoldRewardFlyIcon _adRewardGoldFlyIconPrefab;
        [SerializeField, Min(1)] private int _adRewardGoldFlyIconCount = 7;
        [SerializeField] private float _adRewardGoldFlyDuration = 0.92f;
        [SerializeField] private float _adRewardGoldSpawnInterval = 0.09f;
        [SerializeField] private float _adRewardGoldSpawnPopDuration = 0.16f;
        [SerializeField] private float _adRewardGoldPreMoveDelay = 0.12f;
        [SerializeField] private float _adRewardGoldSpawnSpread = 34f;
        [SerializeField] private float _adRewardGoldDropDistance = 64f;
        [SerializeField] private float _adRewardGoldFlyArcHeight = 142f;
        [SerializeField] private float _adRewardGoldFlySideRandom = 46f;
        [SerializeField] private float _goldReceivePunchScale = 1.22f;
        [SerializeField] private float _goldReceivePunchDuration = 0.24f;

        private Coroutine _scoreAnimationRoutine;
        private Coroutine _gameReadyPromptAnimationRoutine;
        private Coroutine _adRewardGoldFlyRoutine;
        private Coroutine _goldReceivePunchRoutine;
        private Vector3 _scoreTextDefaultScale;
        private Vector3 _goldTextDefaultScale;
        private Color _scoreTextDefaultColor;
        private Vector3 _gameReadyPromptDefaultScale;
        private Vector2 _gameReadyPromptDefaultAnchoredPosition;
        private Color _gameReadyPromptDefaultColor;
        private RectTransform _rootRectTransform;
        private Action _onGameReadyClicked;
        private Action _onAdRewardClicked;
        private Action _onButtonPressed;
        private Func<int, bool> _onCharacterConfirmed;
        private Func<int, bool> _onCharacterPurchased;
        private readonly List<UI_GoldRewardFlyIcon> _adRewardGoldFlyIcons = new List<UI_GoldRewardFlyIcon>(8);
        private readonly List<UI_CharacterPage> _characterPages = new List<UI_CharacterPage>(8);
        private readonly List<int> _characterPrices = new List<int>(8);
        private readonly List<bool> _characterOwned = new List<bool>(8);
        private readonly List<string> _characterLocalizedNames = new List<string>(8);
        private readonly List<string> _characterLocalizedDescriptions = new List<string>(8);
        private UI_ToastMessage _toastMessage;
        private UI_CharacterUnlockFX _characterUnlockFX;
        private int _selectedCharacterSkinId;
        private int _currentGold;

        protected override void Awake()
        {
            base.Awake();
            _rootRectTransform = (RectTransform)transform;
            PrepareCharacterPageTemplate();
            PrepareAdRewardGoldFlyIcons();
            _toastMessage = Instantiate(_toastMessagePrefab, transform, false);
            _toastMessage.HideImmediate();
            _characterUnlockFX = Instantiate(_characterUnlockFXPrefab, _characterSelectPanel.transform, false);
            _characterUnlockFX.HideImmediate();
            _scoreTextDefaultScale = _scoreText.rectTransform.localScale;
            _goldTextDefaultScale = _goldText.rectTransform.localScale;
            _scoreTextDefaultColor = _scoreText.color;
            _gameReadyPromptDefaultScale = _gameReadyPromptText.rectTransform.localScale;
            _gameReadyPromptDefaultAnchoredPosition = _gameReadyPromptText.rectTransform.anchoredPosition;
            _gameReadyPromptDefaultColor = _gameReadyPromptText.color;
            ShowReady();
        }

        public void AddEvents(
            Action onGameReadyClicked,
            Action onAdRewardClicked,
            Func<int, bool> onCharacterConfirmed,
            Func<int, bool> onCharacterPurchased,
            Action onButtonPressed)
        {
            _onGameReadyClicked = onGameReadyClicked;
            _onAdRewardClicked = onAdRewardClicked;
            _onButtonPressed = onButtonPressed;
            _onCharacterConfirmed = onCharacterConfirmed;
            _onCharacterPurchased = onCharacterPurchased;
            ButtonUtils.SetListener(_gameReadyButton, OnGameReadyClicked, _onButtonPressed);
            ButtonUtils.SetListener(_selectCharacterButton, ShowCharacterSelectPanel, _onButtonPressed);
            ButtonUtils.SetListener(_selectCharacterHitAreaButton, ShowCharacterSelectPanel, _onButtonPressed);
            ButtonUtils.SetListener(_adRewardButton, OnAdRewardClicked, _onButtonPressed);
            ButtonUtils.SetListener(_characterConfirmButton, ConfirmSelectedCharacter, _onButtonPressed);
            ButtonUtils.SetListener(_characterBuyButton, BuySelectedCharacter, _onButtonPressed);
            ButtonUtils.SetListener(_characterPrevButton, MoveToPreviousCharacter, _onButtonPressed);
            ButtonUtils.SetListener(_characterNextButton, MoveToNextCharacter, _onButtonPressed);
            _characterScrollRect.onValueChanged.AddListener(OnCharacterScrollValueChanged);
        }

        public void RemoveEvents()
        {
            _onGameReadyClicked = null;
            _onAdRewardClicked = null;
            _onButtonPressed = null;
            _onCharacterConfirmed = null;
            _onCharacterPurchased = null;
            _gameReadyButton.onClick.RemoveAllListeners();
            _selectCharacterButton.onClick.RemoveAllListeners();
            _selectCharacterHitAreaButton.onClick.RemoveAllListeners();
            _adRewardButton.onClick.RemoveAllListeners();
            _characterConfirmButton.onClick.RemoveAllListeners();
            _characterBuyButton.onClick.RemoveAllListeners();
            _characterPrevButton.onClick.RemoveAllListeners();
            _characterNextButton.onClick.RemoveAllListeners();
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

        public void SetAdRewardGoldAmount(int goldAmount)
        {
            _adRewardGoldText.text = $"{Mathf.Max(0, goldAmount)} gold";
        }

        public void SetAdRewardButtonInteractable(bool isInteractable)
        {
            _adRewardButton.interactable = isInteractable;
        }

        public void PlayAdRewardGoldMoveFX()
        {
            StopAdRewardGoldFlyAnimation();
            _adRewardGoldFlyRoutine = StartCoroutine(AdRewardGoldMoveRoutine());
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
            IReadOnlyList<string> localizedNames,
            IReadOnlyList<string> localizedDescriptions)
        {
            ClearGeneratedCharacterPages();
            _characterPrices.Clear();
            _characterOwned.Clear();
            _characterLocalizedNames.Clear();
            _characterLocalizedDescriptions.Clear();

            var pageCount = skinIds.Count;
            pageCount = Mathf.Min(pageCount, sprites.Count);
            pageCount = Mathf.Min(pageCount, prices.Count);
            pageCount = Mathf.Min(pageCount, owned.Count);
            pageCount = Mathf.Min(pageCount, localizedNames.Count);
            pageCount = Mathf.Min(pageCount, localizedDescriptions.Count);
            for (var i = 0; i < pageCount; i++)
            {
                var page = Instantiate(_characterPageTemplate, _characterPageRoot);
                page.gameObject.name = $"CharacterPage_{skinIds[i]}";
                page.Initialize(skinIds[i], sprites[i], OnCharacterPageClicked, _onButtonPressed);
                page.SetOwned(owned[i]);
                page.gameObject.SetActive(true);
                _characterPages.Add(page);
                _characterPrices.Add(prices[i]);
                _characterOwned.Add(owned[i]);
                _characterLocalizedNames.Add(localizedNames[i]);
                _characterLocalizedDescriptions.Add(localizedDescriptions[i]);
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
            _characterPages[index].SetOwned(owned);
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
                PlayCharacterUnlockFX();
                RefreshCharacterPurchaseState();
                return;
            }

            ShowNotEnoughGoldToast();
            RefreshCharacterPurchaseState();
        }

        private void MoveToPreviousCharacter()
        {
            MoveScrollToCharacterIndex(ResolveCurrentCharacterIndex() - 1);
        }

        private void PlayCharacterUnlockFX()
        {
            _characterUnlockFX.PlayAtCenter();
        }

        private void MoveToNextCharacter()
        {
            MoveScrollToCharacterIndex(ResolveCurrentCharacterIndex() + 1);
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

        private void PrepareAdRewardGoldFlyIcons()
        {
            var iconCount = Mathf.Max(1, _adRewardGoldFlyIconCount);
            for (var i = 0; i < iconCount; i++)
            {
                var icon = CreateAdRewardGoldFlyIcon(i);
                var iconTransform = icon.RectTransform;
                iconTransform.SetParent(transform, false);
                iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
                iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
                iconTransform.pivot = new Vector2(0.5f, 0.5f);

                icon.gameObject.SetActive(false);
                _adRewardGoldFlyIcons.Add(icon);
            }
        }

        private UI_GoldRewardFlyIcon CreateAdRewardGoldFlyIcon(int index)
        {
            var prefabIcon = Instantiate(_adRewardGoldFlyIconPrefab);
            prefabIcon.gameObject.name = $"AdRewardGoldFlyIcon_{index:00}";
            return prefabIcon;
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
            _characterLocalizedNames.Clear();
            _characterLocalizedDescriptions.Clear();
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

        private void OnAdRewardClicked()
        {
            _onAdRewardClicked.Invoke();
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
                _characterDescriptionText.text = string.Empty;
                _characterPriceText.text = string.Empty;
                _characterConfirmButtonText.text = string.Empty;
                _characterConfirmButton.interactable = false;
                _characterConfirmButton.gameObject.SetActive(false);
                _characterBuyButton.interactable = false;
                _characterBuyButton.gameObject.SetActive(false);
                _characterPrevButton.gameObject.SetActive(false);
                _characterNextButton.gameObject.SetActive(false);
                return;
            }

            var currentIndex = ResolveCurrentCharacterIndex();
            var isOwned = _characterOwned[currentIndex];
            var price = _characterPrices[currentIndex];
            var isSelected = _characterPages[currentIndex].SkinId == _selectedCharacterSkinId;
            var canSelect = isOwned && !isSelected;

            _characterNameText.text = _characterLocalizedNames[currentIndex];
            _characterDescriptionText.text = _characterLocalizedDescriptions[currentIndex];
            _characterPriceText.text = ResolveCharacterPriceText(price, isOwned);
            _characterConfirmButtonText.text = canSelect ? "Select" : string.Empty;
            _characterConfirmButton.gameObject.SetActive(canSelect);
            _characterConfirmButton.interactable = canSelect;
            _characterBuyButton.gameObject.SetActive(!isOwned);
            _characterBuyButton.interactable = true;
            RefreshCharacterNavigationButtons(currentIndex);
        }

        private void RefreshCharacterNavigationButtons(int currentIndex)
        {
            var maxIndex = _characterPages.Count - 1;
            _characterPrevButton.gameObject.SetActive(currentIndex > 0);
            _characterNextButton.gameObject.SetActive(currentIndex < maxIndex);
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

        private IEnumerator AdRewardGoldMoveRoutine()
        {
            var end = ResolveLocalPosition(_goldText.rectTransform);
            var count = Mathf.Min(Mathf.Max(1, _adRewardGoldFlyIconCount), _adRewardGoldFlyIcons.Count);
            var spawnInterval = Mathf.Max(0f, _adRewardGoldSpawnInterval);
            var popDuration = Mathf.Max(0.01f, _adRewardGoldSpawnPopDuration);
            var preMoveDelay = Mathf.Max(0f, _adRewardGoldPreMoveDelay);
            var duration = Mathf.Max(0.01f, _adRewardGoldFlyDuration);
            var iconLifetime = popDuration + preMoveDelay + duration;
            var totalDuration = spawnInterval * (count - 1) + iconLifetime;
            var elapsed = 0f;
            var completedCount = 0;
            var completed = new bool[count];
            var activated = new bool[count];
            var startPositions = new Vector2[count];
            var dropPositions = new Vector2[count];
            var controlPositions = new Vector2[count];

            for (var i = 0; i < count; i++)
            {
                var icon = _adRewardGoldFlyIcons[i];
                icon.transform.SetAsLastSibling();
                icon.RectTransform.localScale = Vector3.zero;
                icon.gameObject.SetActive(false);

                var start = ResolveAdRewardGoldSpawnPosition();
                var drop = start + Vector2.down * Mathf.Max(0f, _adRewardGoldDropDistance);
                startPositions[i] = start;
                dropPositions[i] = drop;
                controlPositions[i] = ResolveAdRewardGoldFlyControlPosition(drop, end);
            }

            while (completedCount < count && elapsed < totalDuration + 0.1f)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var i = 0; i < count; i++)
                {
                    if (completed[i])
                    {
                        continue;
                    }

                    if (UpdateAdRewardGoldFlyIcon(
                            _adRewardGoldFlyIcons[i],
                            elapsed - i * spawnInterval,
                            popDuration,
                            preMoveDelay,
                            duration,
                            startPositions[i],
                            dropPositions[i],
                            controlPositions[i],
                            end,
                            ref activated[i]))
                    {
                        completed[i] = true;
                        completedCount++;
                        StartGoldReceivePunch();
                    }
                }

                yield return null;
            }

            HideAdRewardGoldFlyIcons();
            _adRewardGoldFlyRoutine = null;
        }

        private bool UpdateAdRewardGoldFlyIcon(
            UI_GoldRewardFlyIcon icon,
            float localElapsed,
            float popDuration,
            float preMoveDelay,
            float duration,
            Vector2 start,
            Vector2 drop,
            Vector2 control,
            Vector2 end,
            ref bool activated)
        {
            if (localElapsed < 0f)
            {
                return false;
            }

            if (!activated)
            {
                activated = true;
                icon.gameObject.SetActive(true);
                icon.transform.SetAsLastSibling();
                icon.RectTransform.anchoredPosition = start;
                icon.RectTransform.localScale = Vector3.zero;
            }

            if (localElapsed < popDuration)
            {
                var popProgress = Mathf.Clamp01(localElapsed / popDuration);
                icon.RectTransform.anchoredPosition = start;
                icon.RectTransform.localScale = Vector3.one * ResolveAdRewardGoldSpawnScale(popProgress);
                return false;
            }

            var moveElapsed = localElapsed - popDuration;
            if (moveElapsed < preMoveDelay)
            {
                icon.RectTransform.anchoredPosition = start;
                icon.RectTransform.localScale = Vector3.one;
                return false;
            }

            var normalized = Mathf.Clamp01((moveElapsed - preMoveDelay) / duration);
            if (normalized >= 1f)
            {
                icon.RectTransform.anchoredPosition = end;
                icon.RectTransform.localScale = Vector3.zero;
                icon.gameObject.SetActive(false);
                return true;
            }

            icon.RectTransform.anchoredPosition = ResolveAdRewardGoldFlyPosition(
                start,
                drop,
                control,
                end,
                normalized);
            icon.RectTransform.localScale = Vector3.one * ResolveAdRewardGoldFlyScale(normalized);
            return false;
        }

        private Vector2 ResolveAdRewardGoldSpawnPosition()
        {
            return _rootRectTransform.rect.center +
                    UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, _adRewardGoldSpawnSpread);
        }

        private float ResolveAdRewardGoldSpawnScale(float normalized)
        {
            if (normalized < 0.72f)
            {
                var popProgress = Mathf.Clamp01(normalized / 0.72f);
                return Mathf.Lerp(0f, 1.1f, Mathf.SmoothStep(0f, 1f, popProgress));
            }

            var settleProgress = Mathf.InverseLerp(0.72f, 1f, normalized);
            return Mathf.Lerp(1.1f, 1f, Mathf.SmoothStep(0f, 1f, settleProgress));
        }

        private float ResolveAdRewardGoldFlyScale(float normalized)
        {
            const float ShrinkStart = 0.84f;
            if (normalized < ShrinkStart)
            {
                return 1f;
            }

            var shrinkProgress = Mathf.InverseLerp(ShrinkStart, 1f, normalized);
            return Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, shrinkProgress));
        }

        private Vector2 ResolveAdRewardGoldFlyControlPosition(Vector2 drop, Vector2 end)
        {
            return (drop + end) * 0.5f +
                    Vector2.up * Mathf.Max(0f, _adRewardGoldFlyArcHeight) +
                    Vector2.right * UnityEngine.Random.Range(-_adRewardGoldFlySideRandom, _adRewardGoldFlySideRandom);
        }

        private Vector2 ResolveAdRewardGoldFlyPosition(
            Vector2 start,
            Vector2 drop,
            Vector2 control,
            Vector2 end,
            float normalized)
        {
            const float DropRatio = 0.32f;
            if (normalized < DropRatio)
            {
                var dropProgress = Mathf.Clamp01(normalized / DropRatio);
                return Vector2.Lerp(start, drop, Mathf.SmoothStep(0f, 1f, dropProgress));
            }

            var flyProgress = Mathf.InverseLerp(DropRatio, 1f, normalized);
            return ResolveQuadraticBezier(
                drop,
                control,
                end,
                EaseInCubic(flyProgress));
        }

        private float EaseInCubic(float normalized)
        {
            var clamped = Mathf.Clamp01(normalized);
            return clamped * clamped * clamped;
        }

        private Vector2 ResolveQuadraticBezier(
            Vector2 start,
            Vector2 control,
            Vector2 end,
            float normalized)
        {
            var inverse = 1f - normalized;
            return inverse * inverse * start +
                    2f * inverse * normalized * control +
                    normalized * normalized * end;
        }

        private Vector2 ResolveLocalPosition(RectTransform target)
        {
            var camera = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Canvas.worldCamera;
            var screenPosition = RectTransformUtility.WorldToScreenPoint(camera, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootRectTransform,
                screenPosition,
                camera,
                out var localPosition);
            return localPosition;
        }

        private void StopAdRewardGoldFlyAnimation()
        {
            if (_adRewardGoldFlyRoutine != null)
            {
                StopCoroutine(_adRewardGoldFlyRoutine);
                _adRewardGoldFlyRoutine = null;
            }

            HideAdRewardGoldFlyIcons();
        }

        private void HideAdRewardGoldFlyIcons()
        {
            for (var i = 0; i < _adRewardGoldFlyIcons.Count; i++)
            {
                var icon = _adRewardGoldFlyIcons[i];
                icon.RectTransform.localScale = Vector3.one;
                icon.gameObject.SetActive(false);
            }
        }

        private void StartGoldReceivePunch()
        {
            if (_goldReceivePunchRoutine != null)
            {
                StopCoroutine(_goldReceivePunchRoutine);
            }

            _goldReceivePunchRoutine = StartCoroutine(GoldReceivePunchRoutine());
        }

        private IEnumerator GoldReceivePunchRoutine()
        {
            var duration = Mathf.Max(0.01f, _goldReceivePunchDuration);
            var punchScale = _goldTextDefaultScale * Mathf.Max(1f, _goldReceivePunchScale);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var pingPong = normalized < 0.5f ? normalized * 2f : (1f - normalized) * 2f;
                var eased = Mathf.SmoothStep(0f, 1f, pingPong);
                _goldText.rectTransform.localScale = Vector3.Lerp(_goldTextDefaultScale, punchScale, eased);
                yield return null;
            }

            ResetGoldTextAnimation();
            _goldReceivePunchRoutine = null;
        }

        private void ResetGoldTextAnimation()
        {
            _goldText.rectTransform.localScale = _goldTextDefaultScale;
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
            if (_goldReceivePunchRoutine != null)
            {
                StopCoroutine(_goldReceivePunchRoutine);
                _goldReceivePunchRoutine = null;
            }

            ResetGoldTextAnimation();
            StopAdRewardGoldFlyAnimation();
            StopGameReadyPromptAnimation();
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }
    }
}
