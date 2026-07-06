using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace JumJump.Editor
{
    [InitializeOnLoad]
    public static class NexonFontBakeTool
    {
        private const string FontFolderPath = "Assets/Game/03.Resources/Fonts";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 1024;

        private const string RequestFileName = ".codex-bake-nexon-fonts.request";
        private const string CommonRequestFileName = ".codex-bake-fonts.request";
        private const string RunningFileName = ".codex-bake-nexon-fonts.running";
        private const string CommonRunningFileName = ".codex-bake-fonts.running";

        private const string BakeCharacters =
            "0123456789+-.,!?/:()[]{}% " +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "One more try?RestartScoreHighBestPERFECTCOMBOx" +
            "콤보퍼펙트점수최고다시하기일반로켓쉴드";

        static NexonFontBakeTool()
        {
            EditorApplication.delayCall += BakePendingRequest;
        }

        [MenuItem("JumJump/Bake NEXON Font Assets")]
        [MenuItem("JumJump/Assets/Bake NEXON Font Assets")]
        public static void BakeNexonFontAssets()
        {
            BakeFontAsset("NEXONLv1GothicLight.ttf");
            BakeFontAsset("NEXONLv1GothicRegular.ttf");
            BakeFontAsset("NEXONLv1GothicBold.ttf");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("JumJump/Bake Common Font Asset")]
        [MenuItem("JumJump/Assets/Bake Common Font Asset")]
        public static void BakeCommonFontAsset()
        {
            BakeFontAsset("CommonFonts.ttf");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("JumJump/Bake All Font Assets")]
        [MenuItem("JumJump/Assets/Bake All Font Assets")]
        public static void BakeAllFontAssets()
        {
            BakeNexonFontAssets();
            BakeCommonFontAsset();
        }

        [InitializeOnLoadMethod]
        private static void BakePendingRequest()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogWarning($"[{nameof(NexonFontBakeTool)}] Failed to resolve project root.");
                return;
            }

            if (TryBakePendingRequest(
                    projectRoot,
                    CommonRequestFileName,
                    CommonRunningFileName,
                    BakeAllFontAssets))
            {
                return;
            }

            TryBakePendingRequest(projectRoot, RequestFileName, RunningFileName, BakeAllFontAssets);
        }

        private static bool TryBakePendingRequest(
            string projectRoot,
            string requestFileName,
            string runningFileName,
            System.Action bakeAction)
        {
            var requestPath = Path.Combine(projectRoot, requestFileName);
            if (!File.Exists(requestPath))
            {
                return false;
            }

            var runningPath = Path.Combine(projectRoot, runningFileName);
            Debug.Log($"[{nameof(NexonFontBakeTool)}] Font bake request detected: {requestPath}");

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
                return false;
            }

            try
            {
                bakeAction();
            }
            finally
            {
                if (File.Exists(runningPath))
                {
                    File.Delete(runningPath);
                }
            }

            return true;
        }

        private static void BakeFontAsset(string fontFileName)
        {
            var fontPath = $"{FontFolderPath}/{fontFileName}";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[{nameof(NexonFontBakeTool)}] Font not found: {fontPath}");
                return;
            }

            var assetPath = $"{FontFolderPath}/{Path.GetFileNameWithoutExtension(fontFileName)} SDF.asset";
            AssetDatabase.DeleteAsset(assetPath);

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                false);

            if (fontAsset == null)
            {
                Debug.LogError($"[{nameof(NexonFontBakeTool)}] Failed to create TMP font asset: {fontPath}");
                return;
            }

            if (!fontAsset.TryAddCharacters(BakeCharacters, out var missingCharacters, true))
            {
                Debug.LogWarning(
                    $"[{nameof(NexonFontBakeTool)}] Missing glyphs for {font.name}: {missingCharacters}");
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.name = $"{Path.GetFileNameWithoutExtension(fontFileName)} SDF";
            fontAsset.material.name = $"{fontAsset.name} Material";
            fontAsset.atlasTexture.name = $"{fontAsset.name} Atlas";

            fontAsset.creationSettings = new FontAssetCreationSettings
            {
                sourceFontFileGUID = AssetDatabase.AssetPathToGUID(fontPath),
                pointSizeSamplingMode = 1,
                pointSize = SamplingPointSize,
                padding = AtlasPadding,
                paddingMode = 0,
                packingMode = 4,
                atlasWidth = AtlasSize,
                atlasHeight = AtlasSize,
                characterSetSelectionMode = 7,
                characterSequence = BakeCharacters,
                renderMode = (int)GlyphRenderMode.SDFAA,
                includeFontFeatures = true
            };

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);

            Debug.Log($"[{nameof(NexonFontBakeTool)}] Baked TMP font asset: {assetPath}");
        }
    }
}
