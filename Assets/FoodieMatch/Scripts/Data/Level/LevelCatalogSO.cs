using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Data.Level
{
    [CreateAssetMenu(
        fileName = "LevelCatalog",
        menuName = "FoodieMatch/Level/Level Catalog")]
    public sealed class LevelCatalogSO : ScriptableObject
    {
        [SerializeField] private List<LevelDataSO> _levels = new();

        public IReadOnlyList<LevelDataSO> Levels => _levels;

        public LevelValidationResult Validate()
        {
            LevelValidationResult result = new LevelValidationResult();

            if (_levels == null || _levels.Count == 0)
            {
                result.AddError("Level catalog must contain at least one level.");
                return result;
            }

            for (int i = 0; i < _levels.Count; i++)
            {
                LevelDataSO levelData = _levels[i];

                if (levelData == null)
                {
                    result.AddError($"Level at index {i} cannot be null.");
                    continue;
                }

                LevelValidationResult levelResult = levelData.Validate();

                for (int errorIndex = 0;
                     errorIndex < levelResult.Errors.Count;
                    errorIndex++)
                {
                    result.AddError(
                        $"Level {i + 1}: {levelResult.Errors[errorIndex]}");
                }
            }

            return result;
        }
    }
}
