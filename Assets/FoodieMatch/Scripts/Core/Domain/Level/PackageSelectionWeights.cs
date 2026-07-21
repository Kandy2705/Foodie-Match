using System;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class PackageSelectionWeights
    {
        public PackageSelectionWeights(
            int rackRescueWeight,
            int readyNowWeight,
            int topTrayWeight)
        {
            if (rackRescueWeight < 0 || readyNowWeight < 0 || topTrayWeight < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rackRescueWeight),
                    "Package selection weights cannot be negative.");
            }

            long totalWeight = (long)rackRescueWeight + readyNowWeight + topTrayWeight;

            if (totalWeight <= 0)
            {
                throw new ArgumentException(
                    "At least one package selection weight must be greater than zero.");
            }

            RackRescueWeight = rackRescueWeight;
            ReadyNowWeight = readyNowWeight;
            TopTrayWeight = topTrayWeight;
        }

        public int RackRescueWeight { get; }
        public int ReadyNowWeight { get; }
        public int TopTrayWeight { get; }
    }
}
