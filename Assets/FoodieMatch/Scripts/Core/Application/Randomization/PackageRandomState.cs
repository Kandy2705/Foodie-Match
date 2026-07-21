using System;

namespace FoodieMatch.Core.Application.Randomization
{
    public readonly struct PackageRandomState : IEquatable<PackageRandomState>
    {
        public PackageRandomState(int seed, int drawCount)
        {
            if (drawCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(drawCount));
            }

            Seed = seed;
            DrawCount = drawCount;
        }

        public int Seed { get; }
        public int DrawCount { get; }

        public bool Equals(PackageRandomState other)
        {
            return Seed == other.Seed && DrawCount == other.DrawCount;
        }

        public override bool Equals(object obj)
        {
            return obj is PackageRandomState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Seed * 397) ^ DrawCount;
            }
        }

        public static bool operator ==(
            PackageRandomState left,
            PackageRandomState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            PackageRandomState left,
            PackageRandomState right)
        {
            return !left.Equals(right);
        }
    }
}
