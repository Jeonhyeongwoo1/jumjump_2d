using System;
using System.IO;
using JumJump.Controller;
using UnityEditor;
using UnityEngine;

namespace JumJump.Editor
{
    public sealed class BestScoreBreakFXGeneratorWindow : EditorWindow
    {
        private const string PrefabName = "PF_BestScoreBreakFX";
        private const string MarkerFlashName = "MarkerFlash";
        private const string SoftGlowName = "SoftGlow";
        private const string GoldRingBurstName = "GoldRingBurst";
        private const string CelebrationBurstName = "CelebrationBurst";
        private const string NewBestTextName = "NewBestText";
        private const string StarSparklesName = "PS_StarSparkles";
        private const string SeedConfettiName = "PS_SeedConfetti";

        [SerializeField] private Sprite _goldRingBurstSprite;
        [SerializeField] private Sprite _softGoldGlowSprite;
        [SerializeField] private Sprite _starSparkleGoldSprite;
        [SerializeField] private Sprite _starSparkleWhiteSprite;
        [SerializeField] private Sprite _seedConfettiSprite;
        [SerializeField] private Sprite _seedConfettiAtlasSprite;
        [SerializeField] private Sprite _newBestTextENSprite;
        [SerializeField] private Sprite _newBestTextKRSprite;
        [SerializeField] private Sprite _celebrationBurstSprite;
        [SerializeField] private Sprite _markerFlashSprite;

        [SerializeField] private string _outputPrefabFolder = "Assets/JumJump/Prefabs/FX";
        [SerializeField] private string _outputMaterialFolder = "Assets/JumJump/Materials/FX";
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _baseSortingOrder = 60;
        [SerializeField] private bool _useKoreanText;
        [SerializeField] private bool _autoDestroy = true;
        [SerializeField] private float _effectScale = 1f;

        private Material _goldRingMaterial;
        private Material _softGlowMaterial;
        private Material _markerFlashMaterial;
        private Material _celebrationBurstMaterial;
        private Material _starSparkleMaterial;
        private Material _seedConfettiMaterial;
        private Material _newBestTextMaterial;

        [MenuItem("Tools/JumJump/Best Score Break FX Generator")]
        public static void Open()
        {
            GetWindow<BestScoreBreakFXGeneratorWindow>("Best Score Break FX");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);
            _goldRingBurstSprite = (Sprite)EditorGUILayout.ObjectField("goldRingBurstSprite", _goldRingBurstSprite, typeof(Sprite), false);
            _softGoldGlowSprite = (Sprite)EditorGUILayout.ObjectField("softGoldGlowSprite", _softGoldGlowSprite, typeof(Sprite), false);
            _starSparkleGoldSprite = (Sprite)EditorGUILayout.ObjectField("starSparkleGoldSprite", _starSparkleGoldSprite, typeof(Sprite), false);
            _starSparkleWhiteSprite = (Sprite)EditorGUILayout.ObjectField("starSparkleWhiteSprite", _starSparkleWhiteSprite, typeof(Sprite), false);
            _seedConfettiSprite = (Sprite)EditorGUILayout.ObjectField("seedConfettiSprite", _seedConfettiSprite, typeof(Sprite), false);
            _seedConfettiAtlasSprite = (Sprite)EditorGUILayout.ObjectField("seedConfettiAtlasSprite", _seedConfettiAtlasSprite, typeof(Sprite), false);
            _newBestTextENSprite = (Sprite)EditorGUILayout.ObjectField("newBestTextENSprite", _newBestTextENSprite, typeof(Sprite), false);
            _newBestTextKRSprite = (Sprite)EditorGUILayout.ObjectField("newBestTextKRSprite", _newBestTextKRSprite, typeof(Sprite), false);
            _celebrationBurstSprite = (Sprite)EditorGUILayout.ObjectField("celebrationBurstSprite", _celebrationBurstSprite, typeof(Sprite), false);
            _markerFlashSprite = (Sprite)EditorGUILayout.ObjectField("markerFlashSprite", _markerFlashSprite, typeof(Sprite), false);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _outputPrefabFolder = EditorGUILayout.TextField("outputPrefabFolder", _outputPrefabFolder);
            _outputMaterialFolder = EditorGUILayout.TextField("outputMaterialFolder", _outputMaterialFolder);
            _sortingLayerName = EditorGUILayout.TextField("sortingLayerName", _sortingLayerName);
            _baseSortingOrder = EditorGUILayout.IntField("baseSortingOrder", _baseSortingOrder);
            _useKoreanText = EditorGUILayout.Toggle("useKoreanText", _useKoreanText);
            _autoDestroy = EditorGUILayout.Toggle("autoDestroy", _autoDestroy);
            _effectScale = EditorGUILayout.FloatField("effectScale", _effectScale);

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Import Setting 권장값: Texture Type = Sprite (2D and UI), Sprite Mode = Single, Alpha Is Transparency = On, Mesh Type = Full Rect, Filter Mode = Bilinear, Compression = None 또는 High Quality.",
                MessageType.Info);

            if (HasMissingSprites())
            {
                EditorGUILayout.HelpBox("필수 Sprite 일부가 비어 있습니다. 자동 검색을 실행하거나 Inspector에서 수동 할당하세요. PreviewSheet는 자동 검색에서 제외됩니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Find Sprites Automatically"))
            {
                FindSpritesAutomatically();
            }

            if (GUILayout.Button("Create/Refresh Materials"))
            {
                CreateOrRefreshMaterials();
            }

            if (GUILayout.Button("Create Best Score Break FX In Scene"))
            {
                var root = CreateFXObject(true);
                Selection.activeGameObject = root;
            }

            if (GUILayout.Button("Create Best Score Break FX Prefab"))
            {
                CreateBestScoreBreakFXPrefab();
            }

            if (GUILayout.Button("Apply Preset To Selected FX"))
            {
                ApplyPresetToSelectedFX();
            }
        }

        private void FindSpritesAutomatically()
        {
            ApplyRecommendedImportSettingsForBestScoreBreakFX();
            _goldRingBurstSprite = FindSprite("BestScoreFX_GoldRingBurst_1024");
            _softGoldGlowSprite = FindSprite("BestScoreFX_SoftGoldGlow_1024");
            _starSparkleGoldSprite = FindSprite("BestScoreFX_StarSparkle_Gold_512");
            _starSparkleWhiteSprite = FindSprite("BestScoreFX_StarSparkle_White_512");
            _seedConfettiSprite = FindSprite("BestScoreFX_SeedConfetti_Gold_512");
            _seedConfettiAtlasSprite = FindSprite("BestScoreFX_SeedConfettiAtlas_2x2_512");
            _newBestTextENSprite = FindSprite("BestScoreFX_NewBestText_EN_1024x512");
            _newBestTextKRSprite = FindSprite("BestScoreFX_NewBestText_KR_1024x512");
            _celebrationBurstSprite = FindSprite("BestScoreFX_CelebrationBurst_1024");
            _markerFlashSprite = FindSprite("BestScoreFX_MarkerFlash_1024");
        }

        public static void ApplyRecommendedImportSettingsForBestScoreBreakFX()
        {
            var guids = AssetDatabase.FindAssets("BestScoreFX_", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || path.IndexOf("BestScoreFX_PreviewSheet", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var changed = false;
                changed |= SetTextureType(importer, TextureImporterType.Sprite);
                changed |= SetSpriteImportMode(importer, SpriteImportMode.Single);
                changed |= SetAlphaTransparency(importer, true);
                changed |= SetMipMaps(importer, false);
                changed |= SetFilterMode(importer, FilterMode.Bilinear);
                changed |= SetMaxTextureSize(importer, 1024);
                changed |= SetTextureCompression(importer, TextureImporterCompression.CompressedHQ);

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateOrRefreshMaterials()
        {
            EnsureFolder(_outputMaterialFolder);

            _goldRingMaterial = CreateOrUpdateMaterial("MAT_BestScoreFX_GoldRing", _goldRingBurstSprite, false);
            _softGlowMaterial = CreateOrUpdateMaterial("MAT_BestScoreFX_SoftGlow", _softGoldGlowSprite, false);
            _markerFlashMaterial = CreateOrUpdateMaterial("MAT_BestScoreFX_MarkerFlash", _markerFlashSprite, false);
            _celebrationBurstMaterial = CreateOrUpdateMaterial("MAT_BestScoreFX_CelebrationBurst", _celebrationBurstSprite, false);
            _starSparkleMaterial = CreateOrUpdateMaterial("MAT_BestScoreFX_StarSparkle", _starSparkleGoldSprite, true);
            _seedConfettiMaterial = CreateOrUpdateMaterial("MAT_BestScoreFX_SeedConfetti", ResolveSeedSprite(), true);
            _newBestTextMaterial = CreateOrUpdateMaterial("MAT_BestScoreFX_NewBestText", ResolveTextSprite(), false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateBestScoreBreakFXPrefab()
        {
            EnsureFolder(_outputPrefabFolder);
            CreateOrRefreshMaterials();

            var root = CreateFXObject(false);
            root.SetActive(false);
            var prefabPath = $"{_outputPrefabFolder}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = prefab;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ApplyPresetToSelectedFX()
        {
            var fx = Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponent<BestScoreBreakFX>();
            if (fx == null)
            {
                EditorUtility.DisplayDialog("Best Score Break FX", "BestScoreBreakFX 컴포넌트가 있는 오브젝트를 선택하세요.", "OK");
                return;
            }

            CreateOrRefreshMaterials();
            var root = fx.gameObject;
            var markerFlash = GetOrCreateSpriteRenderer(root.transform, MarkerFlashName);
            var softGlow = GetOrCreateSpriteRenderer(root.transform, SoftGlowName);
            var goldRing = GetOrCreateSpriteRenderer(root.transform, GoldRingBurstName);
            var celebrationBurst = GetOrCreateSpriteRenderer(root.transform, CelebrationBurstName);
            var newBestText = GetOrCreateSpriteRenderer(root.transform, NewBestTextName);
            var starSparkles = GetOrCreateParticle(root.transform, StarSparklesName);
            var seedConfetti = GetOrCreateParticle(root.transform, SeedConfettiName);

            ConfigureSpriteRenderers(markerFlash, softGlow, goldRing, celebrationBurst, newBestText);
            ConfigureParticles(starSparkles, seedConfetti);
            AssignFXFields(fx, markerFlash, softGlow, goldRing, celebrationBurst, newBestText, starSparkles, seedConfetti);
            EditorUtility.SetDirty(root);
        }

        private GameObject CreateFXObject(bool registerUndo)
        {
            CreateOrRefreshMaterials();

            var root = new GameObject(PrefabName);
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(root, $"Create {PrefabName}");
            }

            var fx = root.AddComponent<BestScoreBreakFX>();
            var markerFlash = CreateSpriteChild(root.transform, MarkerFlashName);
            var softGlow = CreateSpriteChild(root.transform, SoftGlowName);
            var goldRing = CreateSpriteChild(root.transform, GoldRingBurstName);
            var celebrationBurst = CreateSpriteChild(root.transform, CelebrationBurstName);
            var newBestText = CreateSpriteChild(root.transform, NewBestTextName);
            var starSparkles = CreateParticleChild(root.transform, StarSparklesName);
            var seedConfetti = CreateParticleChild(root.transform, SeedConfettiName);

            ConfigureSpriteRenderers(markerFlash, softGlow, goldRing, celebrationBurst, newBestText);
            ConfigureParticles(starSparkles, seedConfetti);
            AssignFXFields(fx, markerFlash, softGlow, goldRing, celebrationBurst, newBestText, starSparkles, seedConfetti);
            return root;
        }

        private void ConfigureSpriteRenderers(
            SpriteRenderer markerFlash,
            SpriteRenderer softGlow,
            SpriteRenderer goldRing,
            SpriteRenderer celebrationBurst,
            SpriteRenderer newBestText)
        {
            ConfigureSpriteRenderer(markerFlash, _markerFlashSprite, _markerFlashMaterial, _baseSortingOrder);
            ConfigureSpriteRenderer(softGlow, _softGoldGlowSprite, _softGlowMaterial, _baseSortingOrder);
            ConfigureSpriteRenderer(goldRing, _goldRingBurstSprite, _goldRingMaterial, _baseSortingOrder + 1);
            ConfigureSpriteRenderer(celebrationBurst, _celebrationBurstSprite, _celebrationBurstMaterial, _baseSortingOrder + 2);
            ConfigureSpriteRenderer(newBestText, ResolveTextSprite(), _newBestTextMaterial, _baseSortingOrder + 20);
            newBestText.transform.localPosition = Vector3.up * 0.8f;
        }

        private void ConfigureSpriteRenderer(SpriteRenderer renderer, Sprite sprite, Material material, int sortingOrder)
        {
            renderer.sprite = sprite;
            renderer.material = material;
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;
            renderer.enabled = false;
        }

        private void ConfigureParticles(ParticleSystem starSparkles, ParticleSystem seedConfetti)
        {
            ConfigureStarSparkles(starSparkles);
            ConfigureSeedConfetti(seedConfetti);
        }

        private void ConfigureStarSparkles(ParticleSystem particleSystem)
        {
            ConfigureBase(
                particleSystem,
                0.5f,
                new ParticleSystem.MinMaxCurve(0.35f, 0.75f),
                new ParticleSystem.MinMaxCurve(1.2f, 2.4f),
                new ParticleSystem.MinMaxCurve(0.08f, 0.18f),
                0.15f,
                24);
            var main = particleSystem.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ConfigureBurst(particleSystem, 10, 16);
            ConfigureCircleShape(particleSystem, 0.2f);
            ConfigureVelocity(particleSystem, new ParticleSystem.MinMaxCurve(-0.75f, 0.75f), new ParticleSystem.MinMaxCurve(0.4f, 1.1f));
            ConfigureSizeOverLifetime(particleSystem, Key(0f, 0.7f), Key(0.35f, 1.15f), Key(1f, 0.5f));
            ConfigureColorOverLifetime(
                particleSystem,
                new Color(1f, 0.9f, 0.36f, 0f),
                new Color(1f, 0.96f, 0.52f, 1f),
                new Color(1f, 0.96f, 0.78f, 0.7f),
                new Color(1f, 0.96f, 0.78f, 0f));
            ConfigureSpriteParticleRenderer(particleSystem, _starSparkleMaterial, _starSparkleGoldSprite, _starSparkleWhiteSprite);
        }

        private void ConfigureSeedConfetti(ParticleSystem particleSystem)
        {
            ConfigureBase(
                particleSystem,
                0.6f,
                new ParticleSystem.MinMaxCurve(0.45f, 0.9f),
                new ParticleSystem.MinMaxCurve(0.9f, 1.8f),
                new ParticleSystem.MinMaxCurve(0.06f, 0.14f),
                0.35f,
                20);
            var main = particleSystem.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ConfigureBurst(particleSystem, 8, 14);
            ConfigureCircleShape(particleSystem, 0.17f);
            ConfigureVelocity(particleSystem, new ParticleSystem.MinMaxCurve(-0.65f, 0.65f), new ParticleSystem.MinMaxCurve(0.25f, 0.95f));
            ConfigureRotationOverLifetime(particleSystem);
            ConfigureColorOverLifetime(
                particleSystem,
                new Color(1f, 0.8f, 0.2f, 0f),
                new Color(1f, 0.86f, 0.34f, 1f),
                new Color(1f, 0.9f, 0.48f, 0.8f),
                new Color(1f, 0.9f, 0.48f, 0f));

            if (_seedConfettiAtlasSprite != null)
            {
                ConfigureGridParticleRenderer(particleSystem, _seedConfettiMaterial, _seedConfettiAtlasSprite);
                return;
            }

            ConfigureSpriteParticleRenderer(particleSystem, _seedConfettiMaterial, _seedConfettiSprite);
        }

        private void ConfigureBase(
            ParticleSystem particleSystem,
            float duration,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            float gravity,
            int maxParticles)
        {
            var main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = duration;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = Mathf.Max(1, maxParticles);
        }

        private void ConfigureBurst(ParticleSystem particleSystem, short minCount, short maxCount)
        {
            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, minCount, maxCount) });
        }

        private void ConfigureCircleShape(ParticleSystem particleSystem, float radius)
        {
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;
        }

        private void ConfigureVelocity(ParticleSystem particleSystem, ParticleSystem.MinMaxCurve x, ParticleSystem.MinMaxCurve y)
        {
            var velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = x;
            velocity.y = y;
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        private void ConfigureRotationOverLifetime(ParticleSystem particleSystem)
        {
            var rotation = particleSystem.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        }

        private void ConfigureSizeOverLifetime(ParticleSystem particleSystem, Keyframe start, Keyframe middle, Keyframe end)
        {
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(start, middle, end));
        }

        private void ConfigureColorOverLifetime(ParticleSystem particleSystem, Color start, Color firstPeak, Color secondPeak, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(firstPeak, 0.15f),
                    new GradientColorKey(secondPeak, 0.8f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(firstPeak.a, 0.15f),
                    new GradientAlphaKey(secondPeak.a, 0.8f),
                    new GradientAlphaKey(end.a, 1f)
                });

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void ConfigureSpriteParticleRenderer(ParticleSystem particleSystem, Material material, params Sprite[] sprites)
        {
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = _baseSortingOrder + 10;
            renderer.material = material;

            var textureSheet = particleSystem.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Sprites;
            ClearTextureSheetSprites(textureSheet);

            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    textureSheet.AddSprite(sprites[i]);
                }
            }

            textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0f, Mathf.Max(0f, textureSheet.spriteCount - 1f));
        }

        private void ConfigureGridParticleRenderer(ParticleSystem particleSystem, Material material, Sprite atlasSprite)
        {
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = _baseSortingOrder + 10;
            renderer.material = material;

            var textureSheet = particleSystem.textureSheetAnimation;
            textureSheet.enabled = true;
            ClearTextureSheetSprites(textureSheet);
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.numTilesX = 2;
            textureSheet.numTilesY = 2;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 3f);

            if (material != null && atlasSprite != null)
            {
                SetMaterialTexture(material, atlasSprite.texture);
            }
        }

        private void ClearTextureSheetSprites(ParticleSystem.TextureSheetAnimationModule textureSheet)
        {
            while (textureSheet.spriteCount > 0)
            {
                textureSheet.RemoveSprite(0);
            }
        }

        private void AssignFXFields(
            BestScoreBreakFX fx,
            SpriteRenderer markerFlash,
            SpriteRenderer softGlow,
            SpriteRenderer goldRing,
            SpriteRenderer celebrationBurst,
            SpriteRenderer newBestText,
            ParticleSystem starSparkles,
            ParticleSystem seedConfetti)
        {
            var serializedObject = new SerializedObject(fx);
            serializedObject.FindProperty("_markerFlashRenderer").objectReferenceValue = markerFlash;
            serializedObject.FindProperty("_softGlowRenderer").objectReferenceValue = softGlow;
            serializedObject.FindProperty("_goldRingRenderer").objectReferenceValue = goldRing;
            serializedObject.FindProperty("_celebrationBurstRenderer").objectReferenceValue = celebrationBurst;
            serializedObject.FindProperty("_newBestTextRenderer").objectReferenceValue = newBestText;
            serializedObject.FindProperty("_starSparkles").objectReferenceValue = starSparkles;
            serializedObject.FindProperty("_seedConfetti").objectReferenceValue = seedConfetti;
            serializedObject.FindProperty("_newBestTextEnglishSprite").objectReferenceValue = _newBestTextENSprite;
            serializedObject.FindProperty("_newBestTextKoreanSprite").objectReferenceValue = _newBestTextKRSprite;
            serializedObject.FindProperty("_useKoreanText").boolValue = _useKoreanText;
            serializedObject.FindProperty("_autoDestroy").boolValue = _autoDestroy;
            serializedObject.FindProperty("_effectScale").floatValue = Mathf.Max(0.01f, _effectScale);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private SpriteRenderer CreateSpriteChild(Transform parent, string objectName)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            return child.AddComponent<SpriteRenderer>();
        }

        private SpriteRenderer GetOrCreateSpriteRenderer(Transform parent, string objectName)
        {
            var child = parent.Find(objectName);
            if (child == null)
            {
                return CreateSpriteChild(parent, objectName);
            }

            var spriteRenderer = child.GetComponent<SpriteRenderer>();
            return spriteRenderer != null ? spriteRenderer : child.gameObject.AddComponent<SpriteRenderer>();
        }

        private ParticleSystem CreateParticleChild(Transform parent, string objectName)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            return child.AddComponent<ParticleSystem>();
        }

        private ParticleSystem GetOrCreateParticle(Transform parent, string objectName)
        {
            var child = parent.Find(objectName);
            if (child == null)
            {
                return CreateParticleChild(parent, objectName);
            }

            var particleSystem = child.GetComponent<ParticleSystem>();
            return particleSystem != null ? particleSystem : child.gameObject.AddComponent<ParticleSystem>();
        }

        private Material CreateOrUpdateMaterial(string materialName, Sprite sprite, bool particleMaterial)
        {
            var path = $"{_outputMaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = particleMaterial ? ResolveParticleShader() : ResolveSpriteShader();
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.name = materialName;
            if (sprite != null)
            {
                SetMaterialTexture(material, sprite.texture);
            }

            ConfigureTransparentMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private Shader ResolveSpriteShader()
        {
            return Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
        }

        private Shader ResolveParticleShader()
        {
            return Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
        }

        private void ConfigureTransparentMaterial(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Glow/Ring을 더 강하게 테스트하려면 머티리얼 Blend Mode를 Additive로 바꿔 비교하세요.
        }

        private void SetMaterialTexture(Material material, Texture texture)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        private Sprite ResolveSeedSprite()
        {
            return _seedConfettiAtlasSprite != null ? _seedConfettiAtlasSprite : _seedConfettiSprite;
        }

        private Sprite ResolveTextSprite()
        {
            if (_useKoreanText && _newBestTextKRSprite != null)
            {
                return _newBestTextKRSprite;
            }

            return _newBestTextENSprite != null ? _newBestTextENSprite : _newBestTextKRSprite;
        }

        private Sprite FindSprite(string assetName)
        {
            var guids = AssetDatabase.FindAssets($"{assetName} t:Sprite", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (IsPreviewSheet(path))
                {
                    continue;
                }

                if (!string.Equals(Path.GetFileNameWithoutExtension(path), assetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return FindSpriteByNameFallback(assetName);
        }

        private Sprite FindSpriteByNameFallback(string assetName)
        {
            var guids = AssetDatabase.FindAssets(assetName, new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (IsPreviewSheet(path))
                {
                    continue;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private bool IsPreviewSheet(string path)
        {
            return path.IndexOf("BestScoreFX_PreviewSheet", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasMissingSprites()
        {
            return _goldRingBurstSprite == null
                || _softGoldGlowSprite == null
                || _starSparkleGoldSprite == null
                || ResolveSeedSprite() == null
                || _newBestTextENSprite == null
                || _newBestTextKRSprite == null
                || _celebrationBurstSprite == null
                || _markerFlashSprite == null;
        }

        private void EnsureFolder(string folderPath)
        {
            var normalizedPath = folderPath.Replace("\\", "/").TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalizedPath))
            {
                return;
            }

            var parts = normalizedPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static bool SetTextureType(TextureImporter importer, TextureImporterType value)
        {
            if (importer.textureType == value)
            {
                return false;
            }

            importer.textureType = value;
            return true;
        }

        private static bool SetSpriteImportMode(TextureImporter importer, SpriteImportMode value)
        {
            if (importer.spriteImportMode == value)
            {
                return false;
            }

            importer.spriteImportMode = value;
            return true;
        }

        private static bool SetAlphaTransparency(TextureImporter importer, bool value)
        {
            if (importer.alphaIsTransparency == value)
            {
                return false;
            }

            importer.alphaIsTransparency = value;
            return true;
        }

        private static bool SetMipMaps(TextureImporter importer, bool value)
        {
            if (importer.mipmapEnabled == value)
            {
                return false;
            }

            importer.mipmapEnabled = value;
            return true;
        }

        private static bool SetFilterMode(TextureImporter importer, FilterMode value)
        {
            if (importer.filterMode == value)
            {
                return false;
            }

            importer.filterMode = value;
            return true;
        }

        private static bool SetMaxTextureSize(TextureImporter importer, int value)
        {
            if (importer.maxTextureSize == value)
            {
                return false;
            }

            importer.maxTextureSize = value;
            return true;
        }

        private static bool SetTextureCompression(TextureImporter importer, TextureImporterCompression value)
        {
            if (importer.textureCompression == value)
            {
                return false;
            }

            importer.textureCompression = value;
            return true;
        }

        private Keyframe Key(float time, float value)
        {
            return new Keyframe(time, value);
        }
    }
}
