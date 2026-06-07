using System;
using System.Collections.Generic;
using JumJump.Factory;
using JumJump.Presenter;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JumJump.Service
{
    public sealed class UIService
    {
        private readonly UIFactory _uiFactory;
        private readonly Dictionary<Type, BaseSceneUI> _activeUIs = new Dictionary<Type, BaseSceneUI>();

        public UIService(UIFactory uiFactory)
        {
            _uiFactory = uiFactory;
        }

        public T Create<T>(string addressableKey) where T : BaseSceneUI
        {
            var ui = _uiFactory.Create<T>(addressableKey);
            if (ui != null)
            {
                _activeUIs[typeof(T)] = ui;
            }
            return ui;
        }

        public T Get<T>() where T : BaseSceneUI
        {
            if (!_activeUIs.TryGetValue(typeof(T), out var ui))
            {
                return null;
            }

            if (ui == null)
            {
                _activeUIs.Remove(typeof(T));
                return null;
            }

            return (T)ui;
        }

        public void Remove<T>() where T : BaseSceneUI
        {
            if (!_activeUIs.TryGetValue(typeof(T), out var ui))
            {
                return;
            }

            _activeUIs.Remove(typeof(T));
            if (ui != null)
            {
                Object.Destroy(ui.gameObject);
            }
        }
    }
}
