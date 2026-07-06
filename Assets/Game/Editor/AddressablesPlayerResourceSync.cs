using System;
using System.Collections.Generic;
using JumJump.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace JumJump.Editor
{
    public static class AddressablesPlayerResourceSync
    {
        private const string PlayerSpriteGroupName = "PlayerSprite";
        private const string PreLoadLabel = "PreLoad";
        private const string ResourceConfigPath = "Assets/Game/03.Resources/Data/ResourceConfigData.asset";

        private static readonly int[] PlayerSkinIds =
        {
            1001,
            1002,
            1003,
            1004,
            1005,
            1006,
            1007,
            1008
        };

        [MenuItem("JumJump/Addressables/Sync Player Resources")]
        public static void SyncPlayerResources()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new InvalidOperationException("AddressableAssetSettings not found.");
            }

            settings.AddLabel(PreLoadLabel);

            var playerSpriteGroup = settings.FindGroup(PlayerSpriteGroupName);
            if (playerSpriteGroup == null)
            {
                throw new InvalidOperationException($"Addressables group not found: {PlayerSpriteGroupName}");
            }

            var playerSpriteKeys = new List<string>(PlayerSkinIds.Length);
            foreach (var skinId in PlayerSkinIds)
            {
                var playerSpriteKey = $"Player_{skinId}.sprite";
                var playerSkinKey = $"PlayerSkin_{skinId}";
                playerSpriteKeys.Add(playerSpriteKey);

                EnsureEntry(
                    settings,
                    playerSpriteGroup,
                    $"Assets/Game/03.Resources/Sprites/Character/{skinId}/Player.png",
                    playerSpriteKey);
                EnsureEntry(
                    settings,
                    playerSpriteGroup,
                    $"Assets/Game/03.Resources/Data/PlayerSkin/{playerSkinKey}.asset",
                    playerSkinKey);
            }

            AddPreLoadLabelToUnlabeledEntries(settings);
            SyncResourceConfig(playerSpriteKeys);

            playerSpriteGroup.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryModified,
                playerSpriteGroup,
                true,
                true);
            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryModified,
                playerSpriteGroup,
                true,
                true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AddressablesPlayerResourceSync] Player resources synced.");
        }

        [MenuItem("JumJump/Addressables/Sync Player Resources And Build")]
        public static void SyncPlayerResourcesAndBuild()
        {
            SyncPlayerResources();

            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"[AddressablesPlayerResourceSync] Addressables build failed: {result.Error}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                throw new InvalidOperationException(result.Error);
            }

            Debug.Log("[AddressablesPlayerResourceSync] Addressables build completed.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void EnsureEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException($"Asset not found: {assetPath}");
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                throw new InvalidOperationException($"Failed to create addressable entry: {assetPath}");
            }

            entry.address = address;
            entry.SetLabel(PreLoadLabel, true, true, false);
        }

        private static void AddPreLoadLabelToUnlabeledEntries(AddressableAssetSettings settings)
        {
            foreach (var group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var entry in group.entries)
                {
                    if (entry == null || entry.labels.Count > 0)
                    {
                        continue;
                    }

                    entry.SetLabel(PreLoadLabel, true, true, false);
                }
            }
        }

        private static void SyncResourceConfig(IReadOnlyList<string> playerSpriteKeys)
        {
            var config = AssetDatabase.LoadAssetAtPath<ResourceConfigData>(ResourceConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException($"ResourceConfigData not found: {ResourceConfigPath}");
            }

            var serializedObject = new SerializedObject(config);
            var keysProperty = serializedObject.FindProperty("_playerSpriteAddressableKeys");
            keysProperty.arraySize = playerSpriteKeys.Count;
            for (var i = 0; i < playerSpriteKeys.Count; i++)
            {
                keysProperty.GetArrayElementAtIndex(i).stringValue = playerSpriteKeys[i];
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }
    }
}
