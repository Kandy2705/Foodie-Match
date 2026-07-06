#if UNITY_EDITOR
using FoodieMatch.Data.Level;
using UnityEditor;
using UnityEngine;

namespace FoodieMatch.Editor.Data.Level
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string WindowTitle = "Level Editor";
        private const int MaxFoodSlotCount = 3;
        private const float MinGrillColumnWidth = 300f;

        private LevelDataSO _levelData;
        private SerializedObject _serializedLevelData;
        private SerializedProperty _levelIdProperty;
        private SerializedProperty _waitingRackCapacityProperty;
        private SerializedProperty _maxPackageSlotCountProperty;
        private SerializedProperty _requiredPackageGenerationConfigProperty;
        private SerializedProperty _grillsProperty;
        private Vector2 _scrollPosition;

        [MenuItem("Foodie Match/Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_levelData == null)
            {
                DrawEmptyState();
                return;
            }

            _serializedLevelData.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawLevelSettings();
            DrawRequiredPackageGenerationConfig();
            DrawGrills();

            EditorGUILayout.EndScrollView();

            _serializedLevelData.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var selectedLevel = (LevelDataSO)EditorGUILayout.ObjectField(
                _levelData,
                typeof(LevelDataSO),
                false,
                GUILayout.MinWidth(240));

            if (selectedLevel != _levelData)
            {
                SetLevelData(selectedLevel);
            }

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                CreateNewLevelData();
            }

            using (new EditorGUI.DisabledScope(_levelData == null))
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveLevelData();
                }

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    ValidateLevelData();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawEmptyState()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Select an existing LevelDataSO or create a new level data asset.",
                MessageType.Info);
        }

        private void SetLevelData(LevelDataSO levelData)
        {
            _levelData = levelData;
            _serializedLevelData = levelData != null ? new SerializedObject(levelData) : null;

            if (_serializedLevelData == null)
            {
                ClearProperties();
                return;
            }

            CacheProperties();
        }

        private void ClearProperties()
        {
            _levelIdProperty = null;
            _waitingRackCapacityProperty = null;
            _maxPackageSlotCountProperty = null;
            _requiredPackageGenerationConfigProperty = null;
            _grillsProperty = null;
        }

        private void CacheProperties()
        {
            _levelIdProperty = _serializedLevelData.FindProperty("_levelId");
            _waitingRackCapacityProperty = _serializedLevelData.FindProperty("_waitingRackCapacity");
            _maxPackageSlotCountProperty = _serializedLevelData.FindProperty("_maxPackageSlotCount");
            _requiredPackageGenerationConfigProperty = _serializedLevelData.FindProperty("_requiredPackageGenerationConfig");
            _grillsProperty = _serializedLevelData.FindProperty("_grills");
        }

        private void DrawLevelSettings()
        {
            EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_levelIdProperty);
            EditorGUILayout.PropertyField(_waitingRackCapacityProperty);
            EditorGUILayout.PropertyField(_maxPackageSlotCountProperty);
            EditorGUILayout.Space();
        }

        private void DrawRequiredPackageGenerationConfig()
        {
            EditorGUILayout.LabelField("Required Package Generation", EditorStyles.boldLabel);

            var initialActivePackageCountProperty =
                _requiredPackageGenerationConfigProperty.FindPropertyRelative("_initialActivePackageCount");
            var minRequiredAmountProperty =
                _requiredPackageGenerationConfigProperty.FindPropertyRelative("_minRequiredAmount");
            var maxRequiredAmountProperty =
                _requiredPackageGenerationConfigProperty.FindPropertyRelative("_maxRequiredAmount");

            EditorGUILayout.PropertyField(initialActivePackageCountProperty);
            EditorGUILayout.PropertyField(minRequiredAmountProperty);
            EditorGUILayout.PropertyField(maxRequiredAmountProperty);
            EditorGUILayout.Space();
        }

        private void DrawGrills()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Grills", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Grill", GUILayout.Width(100)))
            {
                AddGrill();
            }

            EditorGUILayout.EndHorizontal();

            var columnCount = GetGrillColumnCount();

            for (var i = 0; i < _grillsProperty.arraySize; i += columnCount)
            {
                EditorGUILayout.BeginHorizontal();

                for (var column = 0; column < columnCount; column++)
                {
                    var grillIndex = i + column;

                    if (grillIndex >= _grillsProperty.arraySize)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    var grillProperty = _grillsProperty.GetArrayElementAtIndex(grillIndex);
                    DrawGrill(grillIndex, grillProperty);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private int GetGrillColumnCount()
        {
            if (position.width >= MinGrillColumnWidth * 3f)
            {
                return 3;
            }

            if (position.width >= MinGrillColumnWidth * 2f)
            {
                return 2;
            }

            return 1;
        }

        private void DrawGrill(int index, SerializedProperty grillProperty)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(260f), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Grill {index}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                RemoveGrill(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            var positionIndexProperty = grillProperty.FindPropertyRelative("_positionIndex");
            var initialFoodTokenIdsProperty = grillProperty.FindPropertyRelative("_initialFoodTokenIds");
            var platesProperty = grillProperty.FindPropertyRelative("_plates");

            EditorGUILayout.PropertyField(positionIndexProperty);
            DrawTokenSlots("Initial Foods", initialFoodTokenIdsProperty);
            DrawPlates(platesProperty);

            EditorGUILayout.EndVertical();
        }

        private void DrawPlates(SerializedProperty platesProperty)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Plates", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Plate", GUILayout.Width(100)))
            {
                AddPlate(platesProperty);
            }

            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < platesProperty.arraySize; i++)
            {
                var plateProperty = platesProperty.GetArrayElementAtIndex(i);
                DrawPlate(i, platesProperty, plateProperty);
            }
        }

        private void DrawPlate(int index, SerializedProperty platesProperty, SerializedProperty plateProperty)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Plate {index}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                RemovePlate(platesProperty, index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            var foodTokenIdsProperty = plateProperty.FindPropertyRelative("_foodTokenIds");
            DrawTokenSlots("Foods", foodTokenIdsProperty);

            EditorGUILayout.EndVertical();
        }

        private static void DrawTokenSlots(string label, SerializedProperty tokensProperty)
        {
            EnsureTokenSlotCount(tokensProperty);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110));

            for (var i = 0; i < tokensProperty.arraySize; i++)
            {
                var tokenProperty = tokensProperty.GetArrayElementAtIndex(i);
                tokenProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField(tokenProperty.intValue, GUILayout.Width(48)));
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void EnsureTokenSlotCount(SerializedProperty tokensProperty)
        {
            while (tokensProperty.arraySize < MaxFoodSlotCount)
            {
                tokensProperty.InsertArrayElementAtIndex(tokensProperty.arraySize);
                tokensProperty.GetArrayElementAtIndex(tokensProperty.arraySize - 1).intValue = 0;
            }

            while (tokensProperty.arraySize > MaxFoodSlotCount)
            {
                tokensProperty.DeleteArrayElementAtIndex(tokensProperty.arraySize - 1);
            }
        }

        private void AddGrill()
        {
            _grillsProperty.InsertArrayElementAtIndex(_grillsProperty.arraySize);

            var grillProperty = _grillsProperty.GetArrayElementAtIndex(_grillsProperty.arraySize - 1);
            grillProperty.FindPropertyRelative("_positionIndex").intValue = _grillsProperty.arraySize - 1;

            var initialFoodTokenIdsProperty = grillProperty.FindPropertyRelative("_initialFoodTokenIds");
            initialFoodTokenIdsProperty.ClearArray();
            SetDefaultTokenSlots(initialFoodTokenIdsProperty);

            grillProperty.FindPropertyRelative("_plates").ClearArray();
        }

        private void RemoveGrill(int index)
        {
            _grillsProperty.DeleteArrayElementAtIndex(index);
        }

        private static void AddPlate(SerializedProperty platesProperty)
        {
            platesProperty.InsertArrayElementAtIndex(platesProperty.arraySize);

            var plateProperty = platesProperty.GetArrayElementAtIndex(platesProperty.arraySize - 1);
            var foodTokenIdsProperty = plateProperty.FindPropertyRelative("_foodTokenIds");
            foodTokenIdsProperty.ClearArray();
            SetDefaultTokenSlots(foodTokenIdsProperty);
        }

        private static void SetDefaultTokenSlots(SerializedProperty tokensProperty)
        {
            for (var i = 0; i < MaxFoodSlotCount; i++)
            {
                tokensProperty.InsertArrayElementAtIndex(i);
                tokensProperty.GetArrayElementAtIndex(i).intValue = i == 0 ? 1 : 0;
            }
        }

        private static void RemovePlate(SerializedProperty platesProperty, int index)
        {
            platesProperty.DeleteArrayElementAtIndex(index);
        }

        private void CreateNewLevelData()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Level Data",
                "LevelData",
                "asset",
                "Choose where to save the new level data asset.");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var levelData = CreateInstance<LevelDataSO>();
            AssetDatabase.CreateAsset(levelData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetLevelData(levelData);
            Selection.activeObject = levelData;
        }

        private void SaveLevelData()
        {
            _serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(_levelData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Saved level data '{_levelData.name}'.", _levelData);
        }

        private void ValidateLevelData()
        {
            _serializedLevelData.ApplyModifiedProperties();
            var result = _levelData.Validate();

            if (result.IsValid)
            {
                Debug.Log($"Level data '{_levelData.name}' is valid.", _levelData);
                return;
            }

            foreach (var error in result.Errors)
            {
                Debug.LogError(error, _levelData);
            }
        }
    }
}
#endif
