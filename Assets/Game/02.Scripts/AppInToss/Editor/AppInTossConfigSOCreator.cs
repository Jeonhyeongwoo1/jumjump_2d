#if UNITY_EDITOR
using JumJump.Data;
using UnityEditor;
using UnityEngine;

namespace JumJump.Editor
{
    public static class AppInTossConfigSOCreator
    {
        private const string AssetPath = "Assets/Game/03.Resources/AppInTossConfigSO.asset";

        [MenuItem("JumJump/App in Toss/Create Config")]
        public static void CreateConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AppInTossConfigSO>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                return;
            }

            var asset = ScriptableObject.CreateInstance<AppInTossConfigSO>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }
    }
}
#endif
