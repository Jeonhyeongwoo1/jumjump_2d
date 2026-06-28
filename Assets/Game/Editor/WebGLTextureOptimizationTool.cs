using System;
using UnityEditor;
using UnityEngine;

namespace JumJump.Editor
{
    public static class WebGLTextureOptimizationTool
    {
        private const string SpritesRootPath = "Assets/Game/03.Resources/Sprites";
        private const string WebGLPlatformName = "WebGL";
        private const int CompressionQuality = 50;

        private static readonly TextureRule[] TextureRules =
        {
            new TextureRule(SpritesRootPath + "/AppIcon", 1024),
            new TextureRule(SpritesRootPath + "/Map", 1024),
            new TextureRule(SpritesRootPath + "/ScoreBoard", 1024),
            new TextureRule(SpritesRootPath + "/Character", 512),
            new TextureRule(SpritesRootPath + "/Coin", 512),
            new TextureRule(SpritesRootPath + "/Effects", 512),
            new TextureRule(SpritesRootPath + "/Item", 512),
            new TextureRule(SpritesRootPath + "/Parts", 512),
            new TextureRule(SpritesRootPath + "/Platform", 512)
        };

        [MenuItem("JumJump/Optimize/Apply WebGL Texture Import Settings")]
        public static void ApplyWebGLTextureImportSettings()
        {
            var updatedCount = 0;
            var unchangedCount = 0;
            var skippedCount = 0;
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesRootPath });

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        skippedCount++;
                        continue;
                    }

                    if (!TryResolveMaxTextureSize(assetPath, out var maxTextureSize))
                    {
                        skippedCount++;
                        continue;
                    }

                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    var settings = importer.GetPlatformTextureSettings(WebGLPlatformName);
                    var changed = ApplyWebGLSettings(importer, settings, maxTextureSize);
                    if (!changed)
                    {
                        unchangedCount++;
                        continue;
                    }

                    importer.SetPlatformTextureSettings(settings);
                    importer.SaveAndReimport();
                    updatedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                $"[{nameof(WebGLTextureOptimizationTool)}] WebGL texture settings applied. " +
                $"Updated: {updatedCount}, Unchanged: {unchangedCount}, Skipped: {skippedCount}");
        }

        private static bool ApplyWebGLSettings(
            TextureImporter importer,
            TextureImporterPlatformSettings settings,
            int maxTextureSize)
        {
            var changed = false;

            if (settings.overridden != true)
            {
                settings.overridden = true;
                changed = true;
            }

            if (settings.maxTextureSize != maxTextureSize)
            {
                settings.maxTextureSize = maxTextureSize;
                changed = true;
            }

            if (settings.format != TextureImporterFormat.ETC2_RGBA8)
            {
                settings.format = TextureImporterFormat.ETC2_RGBA8;
                changed = true;
            }

            if (settings.textureCompression != TextureImporterCompression.Compressed)
            {
                settings.textureCompression = TextureImporterCompression.Compressed;
                changed = true;
            }

            if (settings.compressionQuality != CompressionQuality)
            {
                settings.compressionQuality = CompressionQuality;
                changed = true;
            }

            if (settings.crunchedCompression)
            {
                settings.crunchedCompression = false;
                changed = true;
            }

            if (settings.allowsAlphaSplitting)
            {
                settings.allowsAlphaSplitting = false;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.isReadable)
            {
                importer.isReadable = false;
                changed = true;
            }

            return changed;
        }

        private static bool TryResolveMaxTextureSize(string assetPath, out int maxTextureSize)
        {
            var normalizedPath = assetPath.Replace("\\", "/");
            for (var i = 0; i < TextureRules.Length; i++)
            {
                if (!normalizedPath.StartsWith(TextureRules[i].PathPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                maxTextureSize = TextureRules[i].MaxTextureSize;
                return true;
            }

            maxTextureSize = 0;
            return false;
        }

        private struct TextureRule
        {
            public string PathPrefix { get; private set; }
            public int MaxTextureSize { get; private set; }

            public TextureRule(string pathPrefix, int maxTextureSize)
            {
                PathPrefix = pathPrefix;
                MaxTextureSize = maxTextureSize;
            }
        }
    }
}
