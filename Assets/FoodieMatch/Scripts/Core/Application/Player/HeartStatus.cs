using System;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class HeartStatus
    {
        public HeartStatus(
            int heartCount,
            int maxHeartCount,
            TimeSpan timeUntilNextHeart,
            TimeSpan recoveryDuration)
        {
            if (maxHeartCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHeartCount),
                    maxHeartCount,
                    "Maximum heart count must be greater than zero.");
            }

            if (heartCount < 0 || heartCount > maxHeartCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heartCount),
                    heartCount,
                    "Heart count must be within the configured range.");
            }

            if (timeUntilNextHeart < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeUntilNextHeart),
                    timeUntilNextHeart,
                    "Time until next heart cannot be negative.");
            }

            if (recoveryDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recoveryDuration),
                    recoveryDuration,
                    "Recovery duration must be greater than zero.");
            }

            HeartCount = heartCount;
            MaxHeartCount = maxHeartCount;
            TimeUntilNextHeart = timeUntilNextHeart;
            RecoveryDuration = recoveryDuration;
        }

        public int HeartCount { get; }

        public int MaxHeartCount { get; }

        public TimeSpan TimeUntilNextHeart { get; }

        public TimeSpan RecoveryDuration { get; }

        public bool IsFull => HeartCount >= MaxHeartCount;
    }
}
