using System;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class RequiredPackageGenerationSettings
    {
        public RequiredPackageGenerationSettings(
            int initialActivePackageCount,
            int minRequiredAmount,
            int maxRequiredAmount,
            int waitingRackWeight,
            int activeGrillWeight,
            int topTrayWeight,
            int deepTrayWeight)
        {
            if (initialActivePackageCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialActivePackageCount));
            }

            if (minRequiredAmount < 1 ||
                maxRequiredAmount < minRequiredAmount ||
                maxRequiredAmount > 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minRequiredAmount));
            }

            if (waitingRackWeight < 0 ||
                activeGrillWeight < 0 ||
                topTrayWeight < 0 ||
                deepTrayWeight < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(waitingRackWeight));
            }

            if (waitingRackWeight +
                activeGrillWeight +
                topTrayWeight +
                deepTrayWeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(waitingRackWeight));
            }

            InitialActivePackageCount = initialActivePackageCount;
            MinRequiredAmount = minRequiredAmount;
            MaxRequiredAmount = maxRequiredAmount;
            WaitingRackWeight = waitingRackWeight;
            ActiveGrillWeight = activeGrillWeight;
            TopTrayWeight = topTrayWeight;
            DeepTrayWeight = deepTrayWeight;
        }

        public int InitialActivePackageCount { get; }
        public int MinRequiredAmount { get; }
        public int MaxRequiredAmount { get; }
        public int WaitingRackWeight { get; }
        public int ActiveGrillWeight { get; }
        public int TopTrayWeight { get; }
        public int DeepTrayWeight { get; }
    }
}
