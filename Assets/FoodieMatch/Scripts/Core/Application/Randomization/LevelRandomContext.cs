using System;
using System.Collections.Generic;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Randomization
{
    public sealed class LevelRandomContext
    {
        private LevelRandomContext(
            int packageSeed,
            int foodVisualSeed)
        {
            PackageSeed = packageSeed;
            FoodVisualSeed = foodVisualSeed;
            PackageRandom = new PackageRandom(packageSeed);
        }

        public int PackageSeed { get; }
        public int FoodVisualSeed { get; }
        public PackageRandom PackageRandom { get; }

        public static LevelRandomContext Create(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            LevelRandomSettings settings = level.RandomSettings;
            int packageSeed = SelectPackageSeed(settings);
            int foodVisualSeed = settings.RandomizeFoodVisualsEachRun
                ? CreateRandomSeed()
                : settings.FixedFoodVisualSeed;

            return new LevelRandomContext(packageSeed, foodVisualSeed);
        }

        private static int SelectPackageSeed(LevelRandomSettings settings)
        {
            if (settings.GeneratePackageSeedEachRun)
            {
                return CreateRandomSeed();
            }

            IReadOnlyList<int> packageSeeds = settings.PackageSeeds;

            if (packageSeeds.Count == 1)
            {
                return packageSeeds[0];
            }

            System.Random seedSelector = new(CreateRandomSeed());
            int seedIndex = seedSelector.Next(packageSeeds.Count);
            return packageSeeds[seedIndex];
        }

        private static int CreateRandomSeed()
        {
            return Guid.NewGuid().GetHashCode();
        }
    }
}
