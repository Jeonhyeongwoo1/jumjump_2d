using JumJump.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Factory
{
    public sealed class UIFactory
    {
        private readonly ResourceService _resourceService;
        private readonly IObjectResolver _resolver;

        public UIFactory(ResourceService resourceService, IObjectResolver resolver)
        {
            _resourceService = resourceService;
            _resolver = resolver;
        }

        public T Create<T>(string addressableKey) where T : MonoBehaviour
        {
            var prefab = _resourceService.GetPrefab(addressableKey);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(UIFactory)}] Failed to load UI prefab: {addressableKey}");
                return null;
            }

            var instance = Object.Instantiate(prefab);
            instance.transform.localScale = Vector3.one;
            BindCanvasCamera(instance);
            _resolver.InjectGameObject(instance);

            var ui = instance.GetComponent<T>();
            if (ui == null)
            {
                Debug.LogError($"[{nameof(UIFactory)}] Prefab '{addressableKey}' is missing a {typeof(T).Name} component.");
                Object.Destroy(instance);
                return null;
            }

            return ui;
        }

        private void BindCanvasCamera(GameObject instance)
        {
            var canvas = instance.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning($"[{nameof(UIFactory)}] Prefab has no Canvas component on root; skipping camera bind.");
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UnityEngine.Camera.main;
            if (canvas.worldCamera == null)
            {
                Debug.LogWarning($"[{nameof(UIFactory)}] Main camera not found; UI canvas render camera is unset.");
            }
        }
    }
}
