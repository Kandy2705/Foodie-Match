using System;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Randomization
{
    public sealed class LevelRandomContext
    {
        private const int PackageSelectionSalt = 16777619;

        private LevelRandomContext(
            int packageSeed,
            int foodVisualSeed)
        {
            PackageSeed = packageSeed;
            FoodVisualSeed = foodVisualSeed;
            PackageSelectionRandom = new System.Random(
                CreatePackageSelectionRandomSeed(packageSeed));
        }

        public int PackageSeed { get; }
        public int FoodVisualSeed { get; }
        public System.Random PackageSelectionRandom { get; }

        public static LevelRandomContext Create(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            LevelRandomSettings settings = level.RandomSettings;
            int packageSeed = settings.RandomizePackageSelectionEachRun
                ? CreateRandomSeed()
                : settings.PackageSeeds[0];
            int foodVisualSeed = settings.RandomizeFoodVisualsEachRun
                ? CreateRandomSeed()
                : settings.FixedFoodVisualSeed;

            return new LevelRandomContext(packageSeed, foodVisualSeed);
        }

        private static int CreatePackageSelectionRandomSeed(int packageSeed)
        {
            unchecked
            {
                int randomSeed = packageSeed;
                randomSeed ^= PackageSelectionSalt + (randomSeed << 6) + (randomSeed >> 2);
                randomSeed *= 397;
                randomSeed ^= randomSeed >> 16;
                return randomSeed;
            }
        }

        private static int CreateRandomSeed()
        {
            return Guid.NewGuid().GetHashCode();
        }
    }
}
