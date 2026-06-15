using JumJump.Controller;
using UnityEditor;
using UnityEngine;

namespace JumJump.Editor
{
    public sealed class CoinCollectFXGeneratorWindow : EditorWindow
    {
        private const string PrefabName = "PF_CoinCollectFX";
        private const string GlowRingName = "PS_GlowRing";
        private const string StarBurstName = "PS_StarBurst";
        private const string SparkleBurstName = "PS_SparkleBurst";
        private const string FloatingDotsName = "PS_FloatingDots";
        private const string CoinPopName = "PS_CoinPop";

        [SerializeField] private Sprite _coinSprite;
        [SerializeField] private Sprite _glowRingSprite;
        [SerializeField] private Sprite _starBurstSprite;
        [SerializeField] private Sprite _sparkleSprite;
        [SerializeField] private Sprite _sparkleAltSprite;
        [SerializeField] private Sprite _floatingDotSprite;
        [SerializeField] private string _outputPrefabFolder = "Assets/JumJump/Prefabs/FX";
        [SerializeField] private string _outputMaterialFolder = "Assets/JumJump/Materials/FX";
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _fxSortingOrder = 50;
        [SerializeField] private bool _autoDestroy = true;
        [SerializeField] private float _destroyDelay = 0.8f;

        private Material _glowRingMaterial;
        private Material _starBurstMaterial;
        private Material _sparkleMaterial;
        private Material _floatingDotMaterial;
        private Material _coinPopMaterial;

        [MenuItem("Tools/JumJump/Coin Collect FX Generator")]
        public static void Open()
        {
            GetWindow<CoinCollectFXGeneratorWindow>("Coin Collect FX");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);
            _coinSprite = (Sprite)EditorGUILayout.ObjectField("coinSprite", _coinSprite, typeof(Sprite), false);
            _glowRingSprite = (Sprite)EditorGUILayout.ObjectField("glowRingSprite", _glowRingSprite, typeof(Sprite), false);
            _starBurstSprite = (Sprite)EditorGUILayout.ObjectField("starBurstSprite", _starBurstSprite, typeof(Sprite), false);
            _sparkleSprite = (Sprite)EditorGUILayout.ObjectField("sparkleSprite", _sparkleSprite, typeof(Sprite), false);
            _sparkleAltSprite = (Sprite)EditorGUILayout.ObjectField("sparkleAltSprite", _sparkleAltSprite, typeof(Sprite), false);
            _floatingDotSprite = (Sprite)EditorGUILayout.ObjectField("floatingDotSprite", _floatingDotSprite, typeof(Sprite), false);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _outputPrefabFolder = EditorGUILayout.TextField("outputPrefabFolder", _outputPrefabFolder);
            _outputMaterialFolder = EditorGUILayout.TextField("outputMaterialFolder", _outputMaterialFolder);
            _sortingLayerName = EditorGUILayout.TextField("sortingLayerName", _sortingLayerName);
            _fxSortingOrder = EditorGUILayout.IntField("fxSortingOrder", _fxSortingOrder);
            _autoDestroy = EditorGUILayout.Toggle("autoDestroy", _autoDestroy);
            _destroyDelay = EditorGUILayout.FloatField("destroyDelay", _destroyDelay);

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Import Setting 권장값: Texture Type = Sprite (2D and UI), Sprite Mode = Single, Alpha Is Transparency = On, Mesh Type = Full Rect, Filter Mode = Bilinear, Compression = None 또는 High Quality.",
                MessageType.Info);

            if (HasMissingSprites())
            {
                EditorGUILayout.HelpBox("일부 Sprite가 비어 있습니다. 자동 검색을 실행하거나 Inspector에서 수동 할당한 뒤 생성하세요.", MessageType.Warning);
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

            if (GUILayout.Button("Create Coin Collect FX In Scene"))
            {
                var root = CreateFXObject(true);
                Selection.activeGameObject = root;
            }

            if (GUILayout.Button("Create Coin Collect FX Prefab"))
            {
                CreateCoinCollectFXPrefab();
            }

            if (GUILayout.Button("Apply Preset To Selected FX"))
            {
                ApplyPresetToSelectedFX();
            }
        }

        private void FindSpritesAutomatically()
        {
            _coinSprite = FindSprite("ResourceBar_Icon_Gold", "Economy_Coin_02_Gold", "coin_2", "Gold_1");
            _glowRingSprite = FindSprite("golden_halo_with_radiant_glow", "Gear_Ring_01_Gold", "HayGlowBokeh", "HayGlowHorizontalOval");
            _starBurstSprite = FindSprite("radiant_golden_starburst_sparkle_effect", "SampleEffect_Sparkle_4", "HaySparkleFourPoint", "JumpParticle_5");
            _sparkleSprite = FindSprite("golden_star_sparkle_icon", "HaySparkleFourPoint", "SampleEffect_Sparkle_1", "JumpParticle_4");
            _sparkleAltSprite = FindSprite("shiny_golden_star_sparkle_icon", "HaySparkleDiamond", "SampleEffect_Sparkle_2", "JumpParticle_3");
            _floatingDotSprite = FindSprite("shiny_golden_star_sparkle_icon", "HayGlowSmallDot", "HayHighlightDot", "HaySparkleDiamond");
        }

        private void CreateOrRefreshMaterials()
        {
            EnsureFolder(_outputMaterialFolder);

            _glowRingMaterial = CreateOrUpdateMaterial("MAT_CoinFX_GlowRing", _glowRingSprite);
            _starBurstMaterial = CreateOrUpdateMaterial("MAT_CoinFX_StarBurst", _starBurstSprite);
            _sparkleMaterial = CreateOrUpdateMaterial("MAT_CoinFX_Sparkle", _sparkleSprite);
            _floatingDotMaterial = CreateOrUpdateMaterial("MAT_CoinFX_FloatingDot", _floatingDotSprite);
            _coinPopMaterial = CreateOrUpdateMaterial("MAT_CoinFX_CoinPop", _coinSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateCoinCollectFXPrefab()
        {
            EnsureFolder(_outputPrefabFolder);
            CreateOrRefreshMaterials();

            var root = CreateFXObject(false);
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
            var fx = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<CoinCollectFX>();
            if (fx == null)
            {
                EditorUtility.DisplayDialog("Coin Collect FX", "CoinCollectFX 컴포넌트가 있는 오브젝트를 선택하세요.", "OK");
                return;
            }

            CreateOrRefreshMaterials();
            var root = fx.gameObject;
            var glowRing = GetOrCreateParticle(root.transform, GlowRingName);
            var starBurst = GetOrCreateParticle(root.transform, StarBurstName);
            var sparkleBurst = GetOrCreateParticle(root.transform, SparkleBurstName);
            var floatingDots = GetOrCreateParticle(root.transform, FloatingDotsName);
            var coinPop = GetOrCreateParticle(root.transform, CoinPopName);

            ConfigureAllParticles(glowRing, starBurst, sparkleBurst, floatingDots, coinPop);
            AssignFXFields(fx, glowRing, starBurst, sparkleBurst, floatingDots, coinPop);
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

            var fx = root.AddComponent<CoinCollectFX>();
            var glowRing = CreateParticleChild(root.transform, GlowRingName);
            var starBurst = CreateParticleChild(root.transform, StarBurstName);
            var sparkleBurst = CreateParticleChild(root.transform, SparkleBurstName);
            var floatingDots = CreateParticleChild(root.transform, FloatingDotsName);
            var coinPop = CreateParticleChild(root.transform, CoinPopName);

            ConfigureAllParticles(glowRing, starBurst, sparkleBurst, floatingDots, coinPop);
            AssignFXFields(fx, glowRing, starBurst, sparkleBurst, floatingDots, coinPop);
            return root;
        }

        private void ConfigureAllParticles(
            ParticleSystem glowRing,
            ParticleSystem starBurst,
            ParticleSystem sparkleBurst,
            ParticleSystem floatingDots,
            ParticleSystem coinPop)
        {
            ConfigureGlowRing(glowRing);
            ConfigureStarBurst(starBurst);
            ConfigureSparkleBurst(sparkleBurst);
            ConfigureFloatingDots(floatingDots);
            ConfigureCoinPop(coinPop);
        }

        private void ConfigureGlowRing(ParticleSystem particleSystem)
        {
            ConfigureBase(particleSystem, 0.25f, new ParticleSystem.MinMaxCurve(0.22f, 0.3f), 0f, new ParticleSystem.MinMaxCurve(0.35f, 0.55f), 0f);
            ConfigureBurst(particleSystem, 1);
            ConfigurePointShape(particleSystem);
            ConfigureSizeOverLifetime(particleSystem, Key(0f, 0.35f), Key(0.4f, 1f), Key(1f, 1.35f));
            ConfigureColorOverLifetime(particleSystem, new Color(1f, 0.78f, 0.18f, 0.9f), new Color(1f, 0.86f, 0.42f, 0.45f), new Color(1f, 0.95f, 0.72f, 0f), 0.55f);
            ConfigureRenderer(particleSystem, _glowRingMaterial, _glowRingSprite);
        }

        private void ConfigureStarBurst(ParticleSystem particleSystem)
        {
            ConfigureBase(particleSystem, 0.25f, new ParticleSystem.MinMaxCurve(0.18f, 0.28f), 0f, new ParticleSystem.MinMaxCurve(0.28f, 0.45f), 0f);
            ConfigureBurst(particleSystem, 1);
            ConfigurePointShape(particleSystem);
            ConfigureSizeOverLifetime(particleSystem, Key(0f, 0.65f), Key(0.3f, 1.05f), Key(1f, 0.85f));
            ConfigureColorOverLifetime(particleSystem, new Color(1f, 0.78f, 0.12f, 0f), new Color(1f, 0.9f, 0.36f, 1f), new Color(1f, 0.96f, 0.68f, 0f), 0.65f);
            ConfigureRenderer(particleSystem, _starBurstMaterial, _starBurstSprite);
        }

        private void ConfigureSparkleBurst(ParticleSystem particleSystem)
        {
            ConfigureBase(particleSystem, 0.35f, new ParticleSystem.MinMaxCurve(0.25f, 0.5f), new ParticleSystem.MinMaxCurve(0.8f, 1.8f), new ParticleSystem.MinMaxCurve(0.045f, 0.12f), 0.15f);
            var main = particleSystem.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ConfigureBurst(particleSystem, 8);
            ConfigureCircleShape(particleSystem, 0.12f);
            ConfigureVelocity(particleSystem, new ParticleSystem.MinMaxCurve(-0.35f, 0.35f), new ParticleSystem.MinMaxCurve(0.15f, 0.75f));
            ConfigureSizeOverLifetime(particleSystem, Key(0f, 0.7f), Key(0.35f, 1.1f), Key(1f, 0.6f));
            ConfigureColorOverLifetime(particleSystem, new Color(1f, 0.77f, 0.18f, 0f), new Color(1f, 0.96f, 0.55f, 1f), new Color(1f, 0.9f, 0.36f, 0f), 0.7f);
            ConfigureRenderer(particleSystem, _sparkleMaterial, _sparkleSprite, _sparkleAltSprite);
        }

        private void ConfigureFloatingDots(ParticleSystem particleSystem)
        {
            ConfigureBase(particleSystem, 0.45f, new ParticleSystem.MinMaxCurve(0.35f, 0.65f), new ParticleSystem.MinMaxCurve(0.25f, 0.7f), new ParticleSystem.MinMaxCurve(0.025f, 0.07f), -0.05f);
            var main = particleSystem.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ConfigureBurst(particleSystem, 6);
            ConfigureCircleShape(particleSystem, 0.16f);
            ConfigureVelocity(particleSystem, new ParticleSystem.MinMaxCurve(-0.15f, 0.15f), new ParticleSystem.MinMaxCurve(0.35f, 0.8f));
            ConfigureSizeOverLifetime(particleSystem, Key(0f, 0.75f), Key(0.45f, 1f), Key(1f, 0.45f));
            ConfigureColorOverLifetime(particleSystem, new Color(1f, 0.83f, 0.25f, 0f), new Color(1f, 0.94f, 0.5f, 0.9f), new Color(1f, 0.93f, 0.58f, 0f), 0.8f);
            ConfigureRenderer(particleSystem, _floatingDotMaterial, _floatingDotSprite);
        }

        private void ConfigureCoinPop(ParticleSystem particleSystem)
        {
            ConfigureBase(particleSystem, 0.25f, new ParticleSystem.MinMaxCurve(0.22f, 0.3f), 0f, new ParticleSystem.MinMaxCurve(0.16f, 0.24f), 0f);
            ConfigureBurst(particleSystem, 1);
            ConfigurePointShape(particleSystem);
            ConfigureSizeOverLifetime(particleSystem, Key(0f, 0.75f), Key(0.35f, 1.2f), Key(1f, 0.6f));
            ConfigureColorOverLifetime(particleSystem, new Color(1f, 1f, 1f, 1f), new Color(1f, 0.92f, 0.48f, 0.75f), new Color(1f, 0.92f, 0.48f, 0f), 0.7f);
            ConfigureRenderer(particleSystem, _coinPopMaterial, _coinSprite);
        }

        private void ConfigureBase(
            ParticleSystem particleSystem,
            float duration,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            float gravity)
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
            main.maxParticles = 16;
        }

        private void ConfigureBurst(ParticleSystem particleSystem, short count)
        {
            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        }

        private void ConfigurePointShape(ParticleSystem particleSystem)
        {
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.001f;
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
            velocity.z = 0f;
        }

        private void ConfigureSizeOverLifetime(ParticleSystem particleSystem, Keyframe start, Keyframe middle, Keyframe end)
        {
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(start, middle, end));
        }

        private void ConfigureColorOverLifetime(ParticleSystem particleSystem, Color start, Color middle, Color end, float middleTime)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(middle, middleTime),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(middle.a, middleTime),
                    new GradientAlphaKey(end.a, 1f)
                });

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void ConfigureRenderer(ParticleSystem particleSystem, Material material, params Sprite[] sprites)
        {
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = _fxSortingOrder;
            renderer.material = material;

            var textureSheet = particleSystem.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Sprites;

            while (textureSheet.spriteCount > 0)
            {
                textureSheet.RemoveSprite(0);
            }

            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    textureSheet.AddSprite(sprites[i]);
                }
            }

            textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0f, Mathf.Max(0f, textureSheet.spriteCount - 1f));
        }

        private void AssignFXFields(
            CoinCollectFX fx,
            ParticleSystem glowRing,
            ParticleSystem starBurst,
            ParticleSystem sparkleBurst,
            ParticleSystem floatingDots,
            ParticleSystem coinPop)
        {
            var serializedObject = new SerializedObject(fx);
            serializedObject.FindProperty("_glowRing").objectReferenceValue = glowRing;
            serializedObject.FindProperty("_starBurst").objectReferenceValue = starBurst;
            serializedObject.FindProperty("_sparkleBurst").objectReferenceValue = sparkleBurst;
            serializedObject.FindProperty("_floatingDots").objectReferenceValue = floatingDots;
            serializedObject.FindProperty("_coinPop").objectReferenceValue = coinPop;
            serializedObject.FindProperty("_autoDestroy").boolValue = _autoDestroy;
            serializedObject.FindProperty("_autoDisable").boolValue = !_autoDestroy;
            serializedObject.FindProperty("_destroyDelay").floatValue = Mathf.Max(0.45f, _destroyDelay);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

        private Material CreateOrUpdateMaterial(string materialName, Sprite sprite)
        {
            var path = $"{_outputMaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(ResolveParticleShader());
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = ResolveParticleShader();
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

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // GlowRing/StarBurst를 더 강하게 테스트하려면 에디터에서 Blend Mode를 Additive로 바꿔 비교하세요.
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

        private Sprite FindSprite(params string[] nameHints)
        {
            for (var i = 0; i < nameHints.Length; i++)
            {
                var sprite = FindSpriteByNameHint(nameHints[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private Sprite FindSpriteByNameHint(string nameHint)
        {
            var guids = AssetDatabase.FindAssets($"{nameHint} t:Sprite", new[] { "Assets" });
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets(nameHint, new[] { "Assets" });
            }

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private bool HasMissingSprites()
        {
            return _coinSprite == null
                || _glowRingSprite == null
                || _starBurstSprite == null
                || _sparkleSprite == null
                || _floatingDotSprite == null;
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

        private Keyframe Key(float time, float value)
        {
            return new Keyframe(time, value);
        }
    }
}
