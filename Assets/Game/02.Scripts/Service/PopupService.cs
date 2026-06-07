using System;
using System.Collections.Generic;
using JumJump.Factory;
using JumJump.Presenter;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class PopupService
    {
        private readonly UIFactory _uiFactory;
        private readonly Dictionary<Type, BasePopup> _cache = new Dictionary<Type, BasePopup>();
        private readonly Stack<BasePopup> _stack = new Stack<BasePopup>();

        public PopupService(UIFactory uiFactory)
        {
            _uiFactory = uiFactory;
        }

        public T Push<T>(string addressableKey) where T : BasePopup
        {
            if (!_cache.TryGetValue(typeof(T), out var popup) || popup == null)
            {
                popup = _uiFactory.Create<T>(addressableKey);
                if (popup == null)
                {
                    Debug.LogError($"[{nameof(PopupService)}] Failed to create popup: {typeof(T).Name}");
                    return null;
                }
                _cache[typeof(T)] = popup;
            }

            if (_stack.Count > 0 && _stack.Peek() == popup)
            {
                return (T)popup;
            }

            popup.gameObject.SetActive(true);
            popup.OnShown();
            _stack.Push(popup);
            return (T)popup;
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;
            var popup = _stack.Pop();
            if (popup == null) return;
            popup.OnHidden();
            popup.gameObject.SetActive(false);
        }

        public void PopAll()
        {
            while (_stack.Count > 0) Pop();
        }

        public bool IsOpen<T>() where T : BasePopup
        {
            return _stack.Count > 0 && _stack.Peek() is T;
        }

        public T Get<T>() where T : BasePopup
        {
            if (_cache.TryGetValue(typeof(T), out var popup) && popup != null)
            {
                return (T)popup;
            }
            return null;
        }
    }
}
