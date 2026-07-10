using System;
using UnityEngine;

namespace FoodieMatch.Data.Level
{
    [Serializable]
    public sealed class RequiredPackageGenerationConfig
    {
        [SerializeField] private int _initialActivePackageCount = 2;
        [SerializeField] private int _minRequiredAmount = 1;
        [SerializeField] private int _maxRequiredAmount = 3;
        [SerializeField] private int _waitingRackWeight = 3;
        [SerializeField] private int _activeGrillWeight = 2;
        [SerializeField] private int _topTrayWeight = 1;
        [SerializeField] private int _deepTrayWeight = 1;

        public int InitialActivePackageCount => _initialActivePackageCount;
        public int MinRequiredAmount => _minRequiredAmount;
        public int MaxRequiredAmount => _maxRequiredAmount;
        public int WaitingRackWeight => _waitingRackWeight;
        public int ActiveGrillWeight => _activeGrillWeight;
        public int TopTrayWeight => _topTrayWeight;
        public int DeepTrayWeight => _deepTrayWeight;

        public void Validate(LevelValidationResult result, int maxPackageSlotCount)
        {
            if (_initialActivePackageCount <= 0)
            {
                result.AddError("InitialActivePackageCount must be greater than 0.");
            }

            if (_initialActivePackageCount > maxPackageSlotCount)
            {
                result.AddError("InitialActivePackageCount cannot be greater than MaxPackageSlotCount.");
            }

            if (_minRequiredAmount < 1)
            {
                result.AddError("MinRequiredAmount must be at least 1.");
            }

            if (_minRequiredAmount > 3)
            {
                result.AddError("MinRequiredAmount cannot be greater than 3.");
            }

            if (_maxRequiredAmount < 1)
            {
                result.AddError("MaxRequiredAmount must be at least 1.");
            }

            if (_maxRequiredAmount > 3)
            {
                result.AddError("MaxRequiredAmount cannot be greater than 3.");
            }

            if (_minRequiredAmount > _maxRequiredAmount)
            {
                result.AddError("MinRequiredAmount cannot be greater than MaxRequiredAmount.");
            }

            ValidateWeights(result);
        }

        private void ValidateWeights(LevelValidationResult result)
        {
            if (_waitingRackWeight < 0 ||
                _activeGrillWeight < 0 ||
                _topTrayWeight < 0 ||
                _deepTrayWeight < 0)
            {
                result.AddError("Required package weights cannot be negative.");
            }

            if (_waitingRackWeight +
                _activeGrillWeight +
                _topTrayWeight +
                _deepTrayWeight <= 0)
            {
                result.AddError(
                    "At least one required package weight must be greater than 0.");
            }
        }
    }
}
