using System;

namespace FoodieMatch.Infrastructure.Advertising
{
    public sealed class LevelPlayAdSettings
    {
        public LevelPlayAdSettings(
            string appKey,
            string rewardedAdUnitId,
            string interstitialAdUnitId)
        {
            if (string.IsNullOrWhiteSpace(appKey))
            {
                throw new ArgumentException(
                    "LevelPlay app key is missing.",
                    nameof(appKey));
            }

            if (string.IsNullOrWhiteSpace(rewardedAdUnitId))
            {
                throw new ArgumentException(
                    "LevelPlay rewarded ad unit ID is missing.",
                    nameof(rewardedAdUnitId));
            }

            if (string.IsNullOrWhiteSpace(interstitialAdUnitId))
            {
                throw new ArgumentException(
                    "LevelPlay interstitial ad unit ID is missing.",
                    nameof(interstitialAdUnitId));
            }

            AppKey = appKey;
            RewardedAdUnitId = rewardedAdUnitId;
            InterstitialAdUnitId = interstitialAdUnitId;
        }

        public string AppKey { get; }

        public string RewardedAdUnitId { get; }

        public string InterstitialAdUnitId { get; }
    }
}
