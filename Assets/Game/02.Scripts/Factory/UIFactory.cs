using JumJump.Service;
using JumJump.Util;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Factory
{
    public sealed class UIFactory
    {
        private readonly ResourceService _resourceService;
        private readonly IObjectResolver _resolver;
        private readonly UnityEngine.Camera _gameCamera;

        public UIFactory(ResourceService resourceService, IObjectResolver resolver, UnityEngine.Camera gameCamera)
        {
            _resourceService = resourceService;
            _resolver = resolver;
            _gameCamera = gameCamera;
        }

        public T Create<T>(string addressableKey) where T : MonoBehaviour
        {
            var prefab = _resourceService.GetPrefab(addressableKey);
            if (prefab == null)
            {
                GameLogger.Error(nameof(UIFactory), $"Failed to load UI prefab: {addressableKey}");
                return null;
            }

            var instance = Object.Instantiate(prefab);
            instance.transform.localScale = Vector3.one;
            BindCanvasCamera(instance);
            _resolver.InjectGameObject(instance);

            var ui = instance.GetComponent<T>();
            return ui;
        }

        private void BindCanvasCamera(GameObject instance)
        {
            var canvas = instance.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _gameCamera;
        }
    }
}
