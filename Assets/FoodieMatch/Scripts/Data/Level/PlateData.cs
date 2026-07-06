using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Data.Level
{
    [Serializable]
    public sealed class PlateData
    {
        [SerializeField] private List<int> _foodTokenIds = new();

        public IReadOnlyList<int> FoodTokenIds => _foodTokenIds;

        public void Validate(LevelValidationResult result, int grillIndex, int plateIndex)
        {
            if (_foodTokenIds == null || _foodTokenIds.Count == 0)
            {
                result.AddError($"Grill {grillIndex}, Plate {plateIndex}: FoodTokenIds cannot be empty.");
                return;
            }

            if (_foodTokenIds.Count > 3)
            {
                result.AddError($"Grill {grillIndex}, Plate {plateIndex}: FoodTokenIds cannot contain more than 3 items.");
            }

            for (var i = 0; i < _foodTokenIds.Count; i++)
            {
                if (_foodTokenIds[i] <= 0)
                {
                    result.AddError($"Grill {grillIndex}, Plate {plateIndex}: FoodTokenIds[{i}] must be greater than 0.");
                }
            }
        }
    }
}
