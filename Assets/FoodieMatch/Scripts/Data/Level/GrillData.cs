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
        [SerializeField] private List<PlateData> _plates = new();

        public int PositionIndex => _positionIndex;
        public IReadOnlyList<int> InitialFoodTokenIds => _initialFoodTokenIds;
        public IReadOnlyList<PlateData> Plates => _plates;

        public void Validate(LevelValidationResult result, int grillIndex)
        {
            if (_positionIndex < 0)
            {
                result.AddError($"Grill {grillIndex}: PositionIndex cannot be negative.");
            }

            ValidateInitialFoodTokens(result, grillIndex);
            ValidatePlates(result, grillIndex);
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

        private void ValidatePlates(LevelValidationResult result, int grillIndex)
        {
            if (_plates == null)
            {
                return;
            }

            for (var i = 0; i < _plates.Count; i++)
            {
                if (_plates[i] == null)
                {
                    result.AddError($"Grill {grillIndex}: Plate {i} cannot be null.");
                    continue;
                }

                _plates[i].Validate(result, grillIndex, i);
            }
        }
    }
}
