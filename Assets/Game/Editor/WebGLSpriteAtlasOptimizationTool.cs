using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace JumJump.Editor
{
    [InitializeOnLoad]
    public static class WebGLSpriteAtlasOptimizationTool
    {
        private const string AtlasFolderPath = "Assets/Game/03.Resources/SpriteAtlases";
        private const string WebGLPlatformName = "WebGL";
        private const int CompressionQuality = 50;
        private const string RequestFileName = ".codex-create-webgl-sprite-atlases.request";
        private const string RunningFileName = ".codex-create-webgl-sprite-atlases.running";

        private static readonly AtlasRule[] AtlasRules =
        {
            new AtlasRule(
                "PlayerDefault",
                1024,
                "Assets/Game/03.Resources/Sprites/Character/1001"),
            new AtlasRule(
                "GameplaySprites",
                2048,
                "Assets/Game/03.Resources/Sprites/Coin",
                "Assets/Game/03.Resources/Sprites/Effects",
                "Assets/Game/03.Resources/Sprites/Item",
                "Assets/Game/03.Resources/Sprites/Parts",
                "Assets/Game/03.Resources/Sprites/Platform"),
            new AtlasRule(
                "ScoreBoard",
                1024,
                "Assets/Game/03.Resources/Sprites/ScoreBoard")
        };

        static WebGLSpriteAtlasOptimizationTool()
        {
            EditorApplication.delayCall += CreatePendingAtlases;
        }

        [MenuItem("JumJump/Optimize/Create WebGL Sprite Atlases")]
        public static void CreateOrUpdateAtlases()
        {
            EnsureAtlasFolder();

            for (var i = 0; i < AtlasRules.Length; i++)
            {
                CreateOrUpdateAtlas(AtlasRules[i]);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[{nameof(WebGLSpriteAtlasOptimizationTool)}] WebGL sprite atlases created or updated.");
        }

        private static void CreateOrUpdateAtlas(AtlasRule rule)
        {
            var atlasPath = $"{AtlasFolderPath}/{rule.Name}.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            ConfigureAtlas(atlas, rule.MaxTextureSize);
            ReplacePackables(atlas, rule.PackablePaths);
            EditorUtility.SetDirty(atlas);
        }

        private static void CreatePendingAtlases()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogWarning($"[{nameof(WebGLSpriteAtlasOptimizationTool)}] Failed to resolve project root.");
                return;
            }

            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (!File.Exists(requestPath))
            {
                return;
            }

            var runningPath = Path.Combine(projectRoot, RunningFileName);
            Debug.Log($"[{nameof(WebGLSpriteAtlasOptimizationTool)}] Sprite atlas request detected: {requestPath}");

            try
            {
                if (File.Exists(runningPath))
                {
                    File.Delete(runningPath);
                }

                File.Move(requestPath, runningPath);
            }
            catch (IOException)
            {
                return;
            }

            try
            {
                CreateOrUpdateAtlases();
            }
            finally
            {
                if (File.Exists(runningPath))
                {
                    File.Delete(runningPath);
                }
            }
        }

        private static void ConfigureAtlas(SpriteAtlas atlas, int maxTextureSize)
        {
            var packingSettings = new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = true,
                padding = 4
            };
            atlas.SetPackingSettings(packingSettings);

            var textureSettings = new SpriteAtlasTextureSettings
            {
                generateMipMaps = false,
                readable = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            };
            atlas.SetTextureSettings(textureSettings);

            var platformSettings = new TextureImporterPlatformSettings
            {
                name = WebGLPlatformName,
                overridden = true,
                maxTextureSize = maxTextureSize,
                format = TextureImporterFormat.ETC2_RGBA8,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = CompressionQuality,
                crunchedCompression = false,
                allowsAlphaSplitting = false
            };
            atlas.SetPlatformSettings(platformSettings);
        }

        private static void ReplacePackables(SpriteAtlas atlas, string[] packablePaths)
        {
            var currentPackables = atlas.GetPackables();
            if (currentPackables.Length > 0)
            {
                atlas.Remove(currentPackables);
            }

            var nextPackables = new List<UnityEngine.Object>(packablePaths.Length);
            for (var i = 0; i < packablePaths.Length; i++)
            {
                var packable = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(packablePaths[i]);
                if (packable == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(WebGLSpriteAtlasOptimizationTool)}] Missing sprite atlas packable: {packablePaths[i]}");
                    continue;
                }

                nextPackables.Add(packable);
            }

            atlas.Add(nextPackables.ToArray());
        }

        private static void EnsureAtlasFolder()
        {
            if (AssetDatabase.IsValidFolder(AtlasFolderPath))
            {
                return;
            }

            const string ResourcesFolderPath = "Assets/Game/03.Resources";
            AssetDatabase.CreateFolder(ResourcesFolderPath, Path.GetFileName(AtlasFolderPath));
        }

        private struct AtlasRule
        {
            public string Name { get; private set; }
            public int MaxTextureSize { get; private set; }
            public string[] PackablePaths { get; private set; }

            public AtlasRule(string name, int maxTextureSize, params string[] packablePaths)
            {
                Name = name;
                MaxTextureSize = maxTextureSize;
                PackablePaths = packablePaths;
            }
        }
    }
}
