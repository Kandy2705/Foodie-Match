using System;

namespace FoodieMatch.Core.Domain.Heart
{
    public sealed class HeartState
    {
        public HeartState(
            int heartCount,
            DateTimeOffset? recoveryStartedAtUtc)
        {
            if (heartCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heartCount),
                    heartCount,
                    "Heart count cannot be negative.");
            }

            HeartCount = heartCount;
            RecoveryStartedAtUtc =
                recoveryStartedAtUtc?.ToUniversalTime();
        }

        public int HeartCount { get; }

        public DateTimeOffset? RecoveryStartedAtUtc { get; }

        public HeartState RefreshRecovery(
            int maxHeartCount,
            TimeSpan recoveryDuration,
            DateTimeOffset utcNow)
        {
            ValidateRecoveryRules(maxHeartCount, recoveryDuration);

            if (HeartCount >= maxHeartCount)
            {
                return HeartCount == maxHeartCount &&
                       RecoveryStartedAtUtc == null
                    ? this
                    : new HeartState(maxHeartCount, null);
            }

            DateTimeOffset currentUtc = utcNow.ToUniversalTime();

            if (!RecoveryStartedAtUtc.HasValue)
            {
                return new HeartState(HeartCount, currentUtc);
            }

            DateTimeOffset recoveryStartedAtUtc =
                RecoveryStartedAtUtc.Value;
            TimeSpan elapsedTime = currentUtc - recoveryStartedAtUtc;

            if (elapsedTime <= TimeSpan.Zero)
            {
                return this;
            }

            long completedRecoveryCount =
                elapsedTime.Ticks / recoveryDuration.Ticks;

            if (completedRecoveryCount <= 0)
            {
                return this;
            }

            int missingHeartCount = maxHeartCount - HeartCount;
            int recoveredHeartCount = (int)Math.Min(
                completedRecoveryCount,
                missingHeartCount);
            int updatedHeartCount = HeartCount + recoveredHeartCount;

            if (updatedHeartCount >= maxHeartCount)
            {
                return new HeartState(maxHeartCount, null);
            }

            long consumedRecoveryTicks = checked(
                recoveredHeartCount * recoveryDuration.Ticks);
            DateTimeOffset updatedRecoveryStart =
                recoveryStartedAtUtc.AddTicks(consumedRecoveryTicks);

            return new HeartState(
                updatedHeartCount,
                updatedRecoveryStart);
        }

        public TimeSpan GetTimeUntilNextHeart(
            TimeSpan recoveryDuration,
            DateTimeOffset utcNow)
        {
            ValidateRecoveryDuration(recoveryDuration);

            if (!RecoveryStartedAtUtc.HasValue)
            {
                return TimeSpan.Zero;
            }

            TimeSpan elapsedTime =
                utcNow.ToUniversalTime() -
                RecoveryStartedAtUtc.Value;

            if (elapsedTime <= TimeSpan.Zero)
            {
                return recoveryDuration;
            }

            if (elapsedTime >= recoveryDuration)
            {
                return TimeSpan.Zero;
            }

            return recoveryDuration - elapsedTime;
        }

        public bool TrySpendHeart(
            int maxHeartCount,
            DateTimeOffset utcNow,
            out HeartState updatedState)
        {
            ValidateMaxHeartCount(maxHeartCount);

            if (HeartCount > maxHeartCount)
            {
                throw new InvalidOperationException(
                    "Heart count cannot exceed the configured maximum.");
            }

            if (HeartCount == 0)
            {
                updatedState = this;
                return false;
            }

            DateTimeOffset? recoveryStartedAtUtc =
                RecoveryStartedAtUtc;

            if (HeartCount == maxHeartCount ||
                !recoveryStartedAtUtc.HasValue)
            {
                recoveryStartedAtUtc = utcNow.ToUniversalTime();
            }

            updatedState = new HeartState(
                HeartCount - 1,
                recoveryStartedAtUtc);
            return true;
        }

        private static void ValidateRecoveryRules(
            int maxHeartCount,
            TimeSpan recoveryDuration)
        {
            ValidateMaxHeartCount(maxHeartCount);
            ValidateRecoveryDuration(recoveryDuration);
        }

        private static void ValidateMaxHeartCount(int maxHeartCount)
        {
            if (maxHeartCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHeartCount),
                    maxHeartCount,
                    "Maximum heart count must be greater than zero.");
            }
        }

        private static void ValidateRecoveryDuration(
            TimeSpan recoveryDuration)
        {
            if (recoveryDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recoveryDuration),
                    recoveryDuration,
                    "Heart recovery duration must be greater than zero.");
            }
        }
    }
}
