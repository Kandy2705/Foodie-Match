using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Data.Level
{
    [Serializable]
    public sealed class TrayData
    {
        [SerializeField] private List<int> _foodTokenIds = new();

        public IReadOnlyList<int> FoodTokenIds => _foodTokenIds;

        public void Validate(LevelValidationResult result, int grillIndex, int trayIndex)
        {
            if (_foodTokenIds == null || _foodTokenIds.Count == 0)
            {
                result.AddError($"Grill {grillIndex}, Tray {trayIndex}: FoodTokenIds cannot be empty.");
                return;
            }

            if (_foodTokenIds.Count > 3)
            {
                result.AddError($"Grill {grillIndex}, Tray {trayIndex}: FoodTokenIds cannot contain more than 3 items.");
            }

            var hasFoodToken = false;

            for (var i = 0; i < _foodTokenIds.Count; i++)
            {
                if (_foodTokenIds[i] < 0)
                {
                    result.AddError($"Grill {grillIndex}, Tray {trayIndex}: FoodTokenIds[{i}] cannot be negative.");
                    continue;
                }

                hasFoodToken |= _foodTokenIds[i] > 0;
            }

            if (!hasFoodToken)
            {
                result.AddError($"Grill {grillIndex}, Tray {trayIndex}: FoodTokenIds must contain at least one item id.");
            }
        }
    }
}
