#if UNITY_EDITOR
using FoodieMatch.Data.Level;
using UnityEditor;
using UnityEngine;

namespace FoodieMatch.Editor.Data.Level
{
    [CustomEditor(typeof(LevelDataSO))]
    public sealed class LevelDataSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Validate Level Data"))
            {
                ValidateLevelData();
            }
        }

        private void ValidateLevelData()
        {
            var levelData = (LevelDataSO)target;
            var result = levelData.Validate();

            if (result.IsValid)
            {
                Debug.Log($"Level data '{levelData.name}' is valid.", levelData);
                return;
            }

            foreach (var error in result.Errors)
            {
                Debug.LogError(error, levelData);
            }
        }
    }
}
#endif
