using System;
using JumJump.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JumJump.Presenter
{
    public sealed class UI_GameOverPopup : BasePopup
    {
        [SerializeField] private Slider _countdownSlider;
        [SerializeField] private TMP_Text _countdownText;
        [SerializeField] private Button _adButton;
        [SerializeField] private Button _closeButton;

        protected override void Awake()
        {
            base.Awake();
            gameObject.SetActive(false);
        }

        public void AddEvents(Action onAdClicked, Action onCloseClicked)
        {
            ButtonUtils.SetListener(_adButton, onAdClicked);
            ButtonUtils.SetListener(_closeButton, onCloseClicked);
        }

        public void RemoveEvents()
        {
            _adButton.onClick.RemoveAllListeners();
            _closeButton.onClick.RemoveAllListeners();
        }

        public void SetCountdown(float normalized, int seconds)
        {
            _countdownSlider.value = normalized;
            _countdownText.text = seconds.ToString();
        }

        public void SetButtonsInteractable(bool isInteractable)
        {
            _adButton.interactable = isInteractable;
            _closeButton.interactable = isInteractable;
        }

        private void OnDestroy()
        {
            _adButton.onClick.RemoveAllListeners();
            _closeButton.onClick.RemoveAllListeners();
        }
    }
}
