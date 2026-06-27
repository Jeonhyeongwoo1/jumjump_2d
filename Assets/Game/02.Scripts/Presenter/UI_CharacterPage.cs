using System;
using JumJump.Util;
using UnityEngine;
using UnityEngine.UI;

namespace JumJump.Presenter
{
    public sealed class UI_CharacterPage : MonoBehaviour
    {
        public int SkinId => _skinId;

        [SerializeField] private Button _button;
        [SerializeField] private Image _characterImage;
        [SerializeField] private Color _ownedColor = Color.white;
        [SerializeField] private Color _lockedColor = Color.black;

        private Action<int> _onClicked;
        private int _skinId;

        public void Initialize(int skinId, Sprite sprite, Action<int> onClicked)
        {
            _skinId = skinId;
            _onClicked = onClicked;
            _characterImage.sprite = sprite;
            ButtonUtils.SetListener(_button, OnClicked);
        }

        public void SetOwned(bool owned)
        {
            _characterImage.color = owned ? _ownedColor : _lockedColor;
        }

        public void RemoveEvents()
        {
            _button.onClick.RemoveAllListeners();
            _onClicked = null;
        }

        private void OnClicked()
        {
            _onClicked.Invoke(_skinId);
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }
    }
}
