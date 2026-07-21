using System;

namespace FoodieMatch.Core.Domain.Level
{
    public sealed class PackageSelectionSettings
    {
        public PackageSelectionSettings(
            PackageSelectionWeights earlyWeights,
            PackageSelectionWeights middleWeights,
            PackageSelectionWeights lateWeights)
        {
            EarlyWeights = earlyWeights ?? throw new ArgumentNullException(nameof(earlyWeights));
            MiddleWeights = middleWeights ?? throw new ArgumentNullException(nameof(middleWeights));
            LateWeights = lateWeights ?? throw new ArgumentNullException(nameof(lateWeights));
        }

        public PackageSelectionWeights EarlyWeights { get; }
        public PackageSelectionWeights MiddleWeights { get; }
        public PackageSelectionWeights LateWeights { get; }

        public LevelProgressPhase GetPhase(float progress)
        {
            if (float.IsNaN(progress) || float.IsInfinity(progress) || progress < 0f || progress > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(progress));
            }

            if (progress < LevelRules.MiddlePhaseStart)
            {
                return LevelProgressPhase.Early;
            }

            return progress < LevelRules.LatePhaseStart
                ? LevelProgressPhase.Middle
                : LevelProgressPhase.Late;
        }

        public PackageSelectionWeights GetWeights(float progress)
        {
            return GetPhase(progress) switch
            {
                LevelProgressPhase.Early => EarlyWeights,
                LevelProgressPhase.Middle => MiddleWeights,
                LevelProgressPhase.Late => LateWeights,
                _ => throw new ArgumentOutOfRangeException(nameof(progress))
            };
        }
    }
}
