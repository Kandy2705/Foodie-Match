using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class LevelRandomSettings
    {
        private readonly ReadOnlyCollection<int> _packageSeeds;

        public LevelRandomSettings(
            IReadOnlyList<int> packageSeeds,
            bool generatePackageSeedEachRun,
            bool randomizeFoodVisualsEachRun,
            int fixedFoodVisualSeed)
        {
            if (packageSeeds == null)
            {
                throw new ArgumentNullException(nameof(packageSeeds));
            }

            if (packageSeeds.Count == 0)
            {
                throw new ArgumentException(
                    "Package seeds must contain at least one seed.",
                    nameof(packageSeeds));
            }

            HashSet<int> uniquePackageSeeds = new(packageSeeds);

            if (uniquePackageSeeds.Count != packageSeeds.Count)
            {
                throw new ArgumentException(
                    "Package seeds cannot contain duplicates.",
                    nameof(packageSeeds));
            }

            List<int> copiedPackageSeeds = new(packageSeeds);
            _packageSeeds = copiedPackageSeeds.AsReadOnly();
            GeneratePackageSeedEachRun = generatePackageSeedEachRun;
            RandomizeFoodVisualsEachRun = randomizeFoodVisualsEachRun;
            FixedFoodVisualSeed = fixedFoodVisualSeed;
        }

        public IReadOnlyList<int> PackageSeeds => _packageSeeds;
        public bool GeneratePackageSeedEachRun { get; }
        public bool RandomizeFoodVisualsEachRun { get; }
        public int FixedFoodVisualSeed { get; }
    }
}
