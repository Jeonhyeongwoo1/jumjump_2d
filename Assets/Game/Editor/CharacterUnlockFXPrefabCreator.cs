using JumJump.Presenter;
using UnityEditor;
using UnityEngine;

namespace JumJump.Editor
{
    public static class CharacterUnlockFXPrefabCreator
    {
        private const string PrefabFolderPath = "Assets/Game/03.Resources/Prefabs/UI";
        private const string FXPrefabPath = PrefabFolderPath + "/UI_CharacterUnlockFX.prefab";
        private const string GameSceneUIPrefabPath = PrefabFolderPath + "/UI_GameScene.prefab";
        private const string FXPrefabName = "UI_CharacterUnlockFX";
        private const string EmbeddedFXName = "CharacterUnlockFX";
        private const string FXPrefabPropertyName = "_characterUnlockFXPrefab";
        private const float FXSize = 360f;

        [MenuItem("Tools/JumJump/Create Character Unlock FX Prefab")]
        public static void CreateOrRefresh()
        {
            EnsureFolder(PrefabFolderPath);
            var fxPrefab = CreateOrRefreshFXPrefab();
            AssignFXPrefabToGameSceneUI(fxPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = fxPrefab.gameObject;
            EditorGUIUtility.PingObject(fxPrefab.gameObject);
            Debug.Log($"Created character unlock FX prefab and assigned it to UI_GameScene: {FXPrefabPath}");
        }

        private static UI_CharacterUnlockFX CreateOrRefreshFXPrefab()
        {
            var root = new GameObject(FXPrefabName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UI_CharacterUnlockFX));
            try
            {
                ApplyUITransformPreset(root);
                var fx = root.GetComponent<UI_CharacterUnlockFX>();
                fx.raycastTarget = false;
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, FXPrefabPath);
                return savedPrefab.GetComponent<UI_CharacterUnlockFX>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssignFXPrefabToGameSceneUI(UI_CharacterUnlockFX fxPrefab)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(GameSceneUIPrefabPath);
            try
            {
                RemoveEmbeddedFXObjects(prefabRoot);
                var view = prefabRoot.GetComponent<UI_GameScene>();
                var serializedView = new SerializedObject(view);
                serializedView.FindProperty(FXPrefabPropertyName).objectReferenceValue = fxPrefab;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, GameSceneUIPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void RemoveEmbeddedFXObjects(GameObject prefabRoot)
        {
            var embeddedFXObjects = prefabRoot.GetComponentsInChildren<UI_CharacterUnlockFX>(true);
            for (var i = 0; i < embeddedFXObjects.Length; i++)
            {
                var fxObject = embeddedFXObjects[i].gameObject;
                if (fxObject.name == FXPrefabName || fxObject.name == EmbeddedFXName)
                {
                    Object.DestroyImmediate(fxObject);
                }
            }
        }

        private static void ApplyUITransformPreset(GameObject root)
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                root.layer = uiLayer;
            }

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(FXSize, FXSize);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private static void EnsureFolder(string folderPath)
        {
            var segments = folderPath.Split('/');
            var currentPath = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var nextPath = currentPath + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
