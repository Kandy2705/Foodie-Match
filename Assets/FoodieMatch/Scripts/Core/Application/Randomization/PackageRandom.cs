using System;

namespace FoodieMatch.Core.Application.Randomization
{
    public sealed class PackageRandom
    {
        private const int PackageSelectionSalt = 16777619;

        private readonly int _seed;
        private System.Random _random;
        private int _drawCount;

        public PackageRandom(int seed)
        {
            _seed = seed;
            _random = CreateRandom(seed);
        }

        public int NextIndex(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _drawCount++;
            return _random.Next(count);
        }

        public long NextWeight(long maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            _drawCount++;
            return (long)(_random.NextDouble() * maxExclusive);
        }

        public PackageRandomState CaptureState()
        {
            return new PackageRandomState(_seed, _drawCount);
        }

        public void RestoreState(PackageRandomState state)
        {
            if (state.Seed != _seed)
            {
                throw new ArgumentException(
                    "Random state seed does not match this package random.",
                    nameof(state));
            }

            _random = CreateRandom(_seed);

            for (int i = 0; i < state.DrawCount; i++)
            {
                _random.Next();
            }

            _drawCount = state.DrawCount;
        }

        private static System.Random CreateRandom(int seed)
        {
            return new System.Random(CreateRandomSeed(seed));
        }

        private static int CreateRandomSeed(int seed)
        {
            unchecked
            {
                int randomSeed = seed;
                randomSeed ^= PackageSelectionSalt + (randomSeed << 6) + (randomSeed >> 2);
                randomSeed *= 397;
                randomSeed ^= randomSeed >> 16;
                return randomSeed;
            }
        }
    }
}
