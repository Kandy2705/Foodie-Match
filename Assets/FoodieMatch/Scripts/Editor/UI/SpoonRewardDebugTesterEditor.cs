using FoodieMatch.UI.Reward;
using UnityEditor;
using UnityEngine;

namespace FoodieMatch.Editor.UI
{
    [CustomEditor(typeof(SpoonRewardDebugTester))]
    public sealed class SpoonRewardDebugTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            SpoonRewardDebugTester tester =
                (SpoonRewardDebugTester)target;
            bool canPlay = Application.isPlaying &&
                           tester.gameObject.scene.IsValid();

            using (new EditorGUI.DisabledScope(!canPlay))
            {
                if (GUILayout.Button("Play Spoon Reward"))
                {
                    tester.Play();
                }
            }

            if (!canPlay)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode, open Home, then select the runtime " +
                    "UIRoot instance to test the Spoon reward.",
                    MessageType.Info);
            }
        }
    }
}
