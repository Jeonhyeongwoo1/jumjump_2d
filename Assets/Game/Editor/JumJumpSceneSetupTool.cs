using System.IO;
using JumJump;
using JumJump.Camera;
using JumJump.Controller;
using JumJump.Factory;
using JumJump.Presenter;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JumJump.Editor
{
    public static class JumJumpSceneSetupTool
    {
        private const string ScenePath = "Assets/Game/01.Scenes/GameScene.unity";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string PrefabFolderPath = "Assets/Game/03.Prefabs";
        private const string PlatformPrefabPath = PrefabFolderPath + "/Platform.prefab";
        private const string PlayerPrefabPath = PrefabFolderPath + "/Player.prefab";

        [MenuItem("JumJump/Setup/Rebuild Game Scene")]
        public static void RebuildGameScene()
        {
            EnsureFolders();

            var platformPrefab = CreatePlatformPrefab();
            var playerPrefab = CreatePlayerPrefab();
            RegisterAddressable(PlatformPrefabPath, PlatformFactory.AddressableKey);
            RegisterAddressable(PlayerPrefabPath, PlayerFactory.AddressableKey);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GameScene";

            var camera = CreateCamera();
            var systems = CreateSystems();
            var hud = CreateHud();

            var followCamera = camera.GetComponent<VerticalFollowCamera>();

            var lifeScope = systems.GetComponent<GameSceneLifeScope>();
            SetObjectField(lifeScope, "_inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            SetObjectField(lifeScope, "_followCamera", followCamera);
            SetObjectField(lifeScope, "_platformPoolRoot", systems.transform.Find("PlatformPoolRoot"));
            SetObjectField(lifeScope, "_gameHudPresenter", hud.GetComponent<GameHudPresenter>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"[{nameof(JumJumpSceneSetupTool)}] Rebuilt scene: {ScenePath}");
        }

        private static void RegisterAddressable(string assetPath, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError($"[{nameof(JumJumpSceneSetupTool)}] Failed to resolve Addressable settings.");
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[{nameof(JumJumpSceneSetupTool)}] Asset not found for addressable: {assetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Game");
            CreateFolderIfMissing("Assets/Game", "01.Scenes");
            CreateFolderIfMissing("Assets/Game", "03.Prefabs");
        }

        private static PlatformController CreatePlatformPrefab()
        {
            var existingPrefab = AssetDatabase.LoadAssetAtPath<PlatformController>(PlatformPrefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            var platformObject = new GameObject("Platform");
            var spriteRenderer = platformObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetBuiltinSprite();
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.color = new Color(0.95f, 0.76f, 0.42f);

            var collider = platformObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var platform = platformObject.AddComponent<PlatformController>();
            SetObjectField(platform, "_spriteRenderer", spriteRenderer);
            SetObjectField(platform, "_landingCollider", collider);

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(platformObject, PlatformPrefabPath);
            Object.DestroyImmediate(platformObject);
            AssetDatabase.SaveAssets();
            return savedPrefab.GetComponent<PlatformController>();
        }

        private static GameObject CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.backgroundColor = new Color(0.62f, 0.85f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0f, 1.2f, -10f);
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<VerticalFollowCamera>();
            return cameraObject;
        }

        private static GameObject CreatePlayerPrefab()
        {
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            var player = new GameObject("Player_Chick");

            var spriteRenderer = player.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetBuiltinSprite();
            spriteRenderer.color = new Color(1f, 0.9f, 0.28f);
            player.transform.localScale = new Vector3(0.48f, 0.48f, 1f);

            player.AddComponent<Player>();

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            AssetDatabase.SaveAssets();
            return savedPrefab;
        }

        private static GameObject CreateSystems()
        {
            var systems = new GameObject("GameSystems");
            systems.AddComponent<GameSceneLifeScope>();

            var poolRoot = new GameObject("PlatformPoolRoot");
            poolRoot.transform.SetParent(systems.transform);
            return systems;
        }

        private static GameObject CreateHud()
        {
            var canvasObject = new GameObject("HudCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var presenter = canvasObject.AddComponent<GameHudPresenter>();

            var scoreText = CreateText(canvasObject.transform, "ScoreText", "0", 96, TextAnchor.UpperCenter);
            Anchor(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(320f, 120f));

            var highScoreText = CreateText(canvasObject.transform, "HighScoreText", "0", 42, TextAnchor.UpperRight);
            Anchor(highScoreText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -48f), new Vector2(260f, 80f));

            var panel = CreatePanel(canvasObject.transform);
            var finalScoreText = CreateText(panel.transform, "FinalScoreText", "0", 84, TextAnchor.MiddleCenter);
            Anchor(finalScoreText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(360f, 110f));

            var restartButton = CreateButton(panel.transform);

            SetObjectField(presenter, "_scoreText", scoreText);
            SetObjectField(presenter, "_highScoreText", highScoreText);
            SetObjectField(presenter, "_gameOverPanel", panel);
            SetObjectField(presenter, "_finalScoreText", finalScoreText);
            SetObjectField(presenter, "_restartButton", restartButton);

            CreateEventSystem();
            panel.SetActive(false);
            return canvasObject;
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(parent, false);
            var image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.58f);
            Anchor(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var uiText = textObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = GetBuiltinFont();
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = Color.white;
            return uiText;
        }

        private static Button CreateButton(Transform parent)
        {
            var buttonObject = new GameObject("RestartButton");
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 0.82f, 0.24f);
            var button = buttonObject.AddComponent<Button>();
            Anchor(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(420f, 120f));

            var label = CreateText(buttonObject.transform, "Label", "Restart", 48, TextAnchor.MiddleCenter);
            label.color = new Color(0.18f, 0.14f, 0.08f);
            Anchor(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void Anchor(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static Sprite GetBuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static Font GetBuiltinFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void SetObjectField(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[{nameof(JumJumpSceneSetupTool)}] Field not found: {target.name}.{fieldName}");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateFolderIfMissing(string parent, string folder)
        {
            var path = Path.Combine(parent, folder).Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
