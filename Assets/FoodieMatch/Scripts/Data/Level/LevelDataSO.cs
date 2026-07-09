using System.Collections.Generic;
using UnityEngine;

namespace FoodieMatch.Data.Level
{
    [CreateAssetMenu(
        fileName = "LevelData",
        menuName = "FoodieMatch/Level/Level Data")]
    public sealed class LevelDataSO : ScriptableObject
    {
        [SerializeField] private int _waitingRackCapacity = 5;
        [SerializeField] private int _maxPackageSlotCount = 4;
        [SerializeField] private RequiredPackageGenerationConfig _requiredPackageGenerationConfig = new();
        [SerializeField] private List<GrillData> _grills = new();

        public int WaitingRackCapacity => _waitingRackCapacity;
        public int MaxPackageSlotCount => _maxPackageSlotCount;
        public RequiredPackageGenerationConfig RequiredPackageGenerationConfig => _requiredPackageGenerationConfig;
        public IReadOnlyList<GrillData> Grills => _grills;

        public LevelValidationResult Validate()
        {
            var result = new LevelValidationResult();

            if (_waitingRackCapacity <= 0)
            {
                result.AddError("WaitingRackCapacity must be greater than 0.");
            }

            if (_maxPackageSlotCount <= 0)
            {
                result.AddError("MaxPackageSlotCount must be greater than 0.");
            }

            if (_requiredPackageGenerationConfig == null)
            {
                result.AddError("RequiredPackageGenerationConfig is required.");
            }
            else
            {
                _requiredPackageGenerationConfig.Validate(result, _maxPackageSlotCount);
            }

            ValidateGrills(result);
            ValidateInitialPackageCapacity(result);

            return result;
        }

        private void ValidateGrills(LevelValidationResult result)
        {
            if (_grills == null || _grills.Count == 0)
            {
                result.AddError("Level must contain at least one grill.");
                return;
            }

            var usedPositionIndexes = new HashSet<int>();

            for (var i = 0; i < _grills.Count; i++)
            {
                if (_grills[i] == null)
                {
                    result.AddError($"Grill {i} cannot be null.");
                    continue;
                }

                _grills[i].Validate(result, i);

                if (_grills[i].PositionIndex >= 0 && !usedPositionIndexes.Add(_grills[i].PositionIndex))
                {
                    result.AddError($"Grill {i}: PositionIndex {_grills[i].PositionIndex} is duplicated.");
                }
            }
        }

        private void ValidateInitialPackageCapacity(LevelValidationResult result)
        {
            if (_requiredPackageGenerationConfig == null || _grills == null)
            {
                return;
            }

            var minimumRequiredItemCount =
                _requiredPackageGenerationConfig.InitialActivePackageCount *
                _requiredPackageGenerationConfig.MinRequiredAmount;

            if (CountFoodTokens() < minimumRequiredItemCount)
            {
                result.AddError("Level does not contain enough food tokens to spawn initial required packages.");
            }
        }

        private int CountFoodTokens()
        {
            var count = 0;

            foreach (var grill in _grills)
            {
                if (grill == null)
                {
                    continue;
                }

                count += CountFoodTokens(grill.InitialFoodTokenIds);

                if (grill.Trays == null)
                {
                    continue;
                }

                foreach (var tray in grill.Trays)
                {
                    count += CountFoodTokens(tray?.FoodTokenIds);
                }
            }

            return count;
        }

        private static int CountFoodTokens(IReadOnlyList<int> foodTokenIds)
        {
            if (foodTokenIds == null)
            {
                return 0;
            }

            var count = 0;

            foreach (var foodTokenId in foodTokenIds)
            {
                if (foodTokenId > 0)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
