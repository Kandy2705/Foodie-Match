using System;
using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Randomization
{
    public sealed class LevelRandomContext
    {
        private const int FoodVisualSalt = 486187739;
        private const int PackageSelectionSalt = 16777619;

        private LevelRandomContext(int runSeed)
        {
            RunSeed = runSeed;
            FoodVisualSeed = DeriveSeed(runSeed, FoodVisualSalt);
            PackageSelectionSeed = DeriveSeed(runSeed, PackageSelectionSalt);
            PackageSelectionRandom = new System.Random(PackageSelectionSeed);
        }

        public int RunSeed { get; }
        public int FoodVisualSeed { get; }
        public int PackageSelectionSeed { get; }
        public System.Random PackageSelectionRandom { get; }

        public static LevelRandomContext Create(LevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            int runSeed = level.UseFixedSeed
                ? level.Seed
                : Guid.NewGuid().GetHashCode();

            return new LevelRandomContext(runSeed);
        }

        private static int DeriveSeed(int runSeed, int salt)
        {
            unchecked
            {
                int mixedSeed = runSeed;
                mixedSeed ^= salt + (mixedSeed << 6) + (mixedSeed >> 2);
                mixedSeed *= 397;
                mixedSeed ^= mixedSeed >> 16;
                return mixedSeed;
            }
        }
    }
}
