using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Data.Level
{
    [Serializable]
    public sealed class GrillData
    {
        [SerializeField] private int _positionIndex;
        [SerializeField] private List<int> _initialFoodTokenIds = new();
        [SerializeField] private List<TrayData> _trays = new();

        public int PositionIndex => _positionIndex;
        public IReadOnlyList<int> InitialFoodTokenIds => _initialFoodTokenIds;
        public IReadOnlyList<TrayData> Trays => _trays;

        public void Validate(LevelValidationResult result, int grillIndex)
        {
            if (_positionIndex < 0)
            {
                result.AddError($"Grill {grillIndex}: PositionIndex cannot be negative.");
            }

            ValidateInitialFoodTokens(result, grillIndex);
            ValidateTrays(result, grillIndex);
        }

        private void ValidateInitialFoodTokens(LevelValidationResult result, int grillIndex)
        {
            if (_initialFoodTokenIds == null || _initialFoodTokenIds.Count == 0)
            {
                result.AddError($"Grill {grillIndex}: InitialFoodTokenIds cannot be empty.");
                return;
            }

            if (_initialFoodTokenIds.Count > 3)
            {
                result.AddError($"Grill {grillIndex}: InitialFoodTokenIds cannot contain more than 3 items.");
            }

            var hasFoodToken = false;

            for (var i = 0; i < _initialFoodTokenIds.Count; i++)
            {
                if (_initialFoodTokenIds[i] < 0)
                {
                    result.AddError($"Grill {grillIndex}: InitialFoodTokenIds[{i}] cannot be negative.");
                    continue;
                }

                hasFoodToken |= _initialFoodTokenIds[i] > 0;
            }

            if (!hasFoodToken)
            {
                result.AddError($"Grill {grillIndex}: InitialFoodTokenIds must contain at least one item id.");
            }
        }

        private void ValidateTrays(LevelValidationResult result, int grillIndex)
        {
            if (_trays == null)
            {
                return;
            }

            for (var i = 0; i < _trays.Count; i++)
            {
                if (_trays[i] == null)
                {
                    result.AddError($"Grill {grillIndex}: Tray {i} cannot be null.");
                    continue;
                }

                _trays[i].Validate(result, grillIndex, i);
            }
        }
    }
}
