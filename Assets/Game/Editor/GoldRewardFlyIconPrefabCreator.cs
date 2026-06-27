using JumJump.Presenter;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JumJump.Editor
{
    public static class GoldRewardFlyIconPrefabCreator
    {
        private const string PrefabFolderPath = "Assets/Game/03.Resources/Prefabs/UI";
        private const string IconPrefabPath = PrefabFolderPath + "/UI_GoldRewardFlyIcon.prefab";
        private const string GameSceneUIPrefabPath = PrefabFolderPath + "/UI_GameScene.prefab";
        private const string IconPrefabName = "UI_GoldRewardFlyIcon";
        private const string IconPrefabPropertyName = "_adRewardGoldFlyIconPrefab";

        [MenuItem("Tools/JumJump/Create Gold Reward Fly Icon Prefab")]
        public static void CreateOrRefresh()
        {
            EnsureFolder(PrefabFolderPath);
            var iconPrefab = CreateOrRefreshIconPrefab();
            AssignIconPrefabToGameSceneUI(iconPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = iconPrefab.gameObject;
            EditorGUIUtility.PingObject(iconPrefab.gameObject);
            Debug.Log($"Created gold reward fly icon prefab and assigned it to UI_GameScene: {IconPrefabPath}");
        }

        private static UI_GoldRewardFlyIcon CreateOrRefreshIconPrefab()
        {
            var root = new GameObject(
                IconPrefabName,
                typeof(RectTransform),
                typeof(CanvasRenderer));
            try
            {
                ApplyUITransformPreset(root);
                var image = root.AddComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.color = Color.white;

                root.AddComponent<UI_GoldRewardFlyIcon>();

                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, IconPrefabPath);
                return savedPrefab.GetComponent<UI_GoldRewardFlyIcon>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssignIconPrefabToGameSceneUI(UI_GoldRewardFlyIcon iconPrefab)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(GameSceneUIPrefabPath);
            try
            {
                var view = prefabRoot.GetComponent<UI_GameScene>();
                var serializedView = new SerializedObject(view);
                serializedView.FindProperty(IconPrefabPropertyName).objectReferenceValue = iconPrefab;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, GameSceneUIPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
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
