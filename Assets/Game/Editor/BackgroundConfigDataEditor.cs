using JumJump.Data;
using UnityEditor;
using UnityEngine;

namespace JumJump.Editor
{
    [CustomEditor(typeof(BackgroundConfigData))]
    public sealed class BackgroundConfigDataEditor : UnityEditor.Editor
    {
        private struct BackgroundPreset
        {
            public float GradientMaxHeight;
            public float SpawnAheadY;
            public float RecycleBelowY;
            public int PoolCount;
            public float[] DepthStartHeights;
        }

        private static readonly BackgroundPreset DefaultPreset = new BackgroundPreset
        {
            GradientMaxHeight = 80f,
            SpawnAheadY = 11f,
            RecycleBelowY = 8f,
            PoolCount = 12,
            DepthStartHeights = new[] { -100f, 16f, 34f, 52f, 70f }
        };

        private static readonly BackgroundPreset FastTestPreset = new BackgroundPreset
        {
            GradientMaxHeight = 10f,
            SpawnAheadY = 4f,
            RecycleBelowY = 4f,
            PoolCount = 24,
            DepthStartHeights = new[] { -100f, 2f, 4f, 6f, 8f }
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Background Test Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Apply Fast Test Preset: shortens depth and gradient height for quick background transition checks.\n" +
                "Restore Defaults: restores launch default values.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Fast Test Preset"))
                {
                    ApplyBackgroundPreset(FastTestPreset);
                }

                if (GUILayout.Button("Restore Defaults"))
                {
                    ApplyBackgroundPreset(DefaultPreset);
                }
            }
        }

        private void ApplyBackgroundPreset(BackgroundPreset preset)
        {
            serializedObject.Update();

            serializedObject.FindProperty("_backgroundGradientMaxHeight").floatValue = preset.GradientMaxHeight;
            serializedObject.FindProperty("_backgroundObjectSpawnAheadY").floatValue = preset.SpawnAheadY;
            serializedObject.FindProperty("_backgroundObjectRecycleBelowY").floatValue = preset.RecycleBelowY;
            serializedObject.FindProperty("_backgroundObjectPoolCount").intValue = preset.PoolCount;

            var layers = serializedObject.FindProperty("_backgroundDepthLayers");
            var count = Mathf.Min(layers.arraySize, preset.DepthStartHeights.Length);
            for (var i = 0; i < count; i++)
            {
                layers.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("_startHeight")
                    .floatValue = preset.DepthStartHeights[i];
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
