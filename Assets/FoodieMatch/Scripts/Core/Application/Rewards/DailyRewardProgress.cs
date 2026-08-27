using System;

namespace FoodieMatch.Core.Application.Rewards
{
    public sealed class DailyRewardProgress
    {
        public DailyRewardProgress(
            long dayNumber,
            int completedLevels,
            int storageUses,
            int swapUses,
            int plateUses,
            int fridgeUses,
            int claimedQuestMask,
            long dailyGiftAvailableAtUnixSeconds,
            int adRewardsClaimed,
            bool finalBonusClaimed)
        {
            if (dayNumber < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(dayNumber));
            }

            if (completedLevels < 0 || storageUses < 0 || swapUses < 0 ||
                plateUses < 0 || fridgeUses < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedLevels),
                    "Quest progress cannot be negative.");
            }

            if (claimedQuestMask < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(claimedQuestMask));
            }

            if (dailyGiftAvailableAtUnixSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dailyGiftAvailableAtUnixSeconds));
            }

            if (adRewardsClaimed < 0 || adRewardsClaimed > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(adRewardsClaimed));
            }

            DayNumber = dayNumber;
            CompletedLevels = completedLevels;
            StorageUses = storageUses;
            SwapUses = swapUses;
            PlateUses = plateUses;
            FridgeUses = fridgeUses;
            ClaimedQuestMask = claimedQuestMask;
            DailyGiftAvailableAtUnixSeconds =
                dailyGiftAvailableAtUnixSeconds;
            AdRewardsClaimed = adRewardsClaimed;
            FinalBonusClaimed = finalBonusClaimed;
        }

        public long DayNumber { get; }
        public int CompletedLevels { get; }
        public int StorageUses { get; }
        public int SwapUses { get; }
        public int PlateUses { get; }
        public int FridgeUses { get; }
        public int ClaimedQuestMask { get; }
        public long DailyGiftAvailableAtUnixSeconds { get; }
        public int AdRewardsClaimed { get; }
        public bool FinalBonusClaimed { get; }

        public static DailyRewardProgress CreateEmpty(long dayNumber)
        {
            return new DailyRewardProgress(
                dayNumber,
                completedLevels: 0,
                storageUses: 0,
                swapUses: 0,
                plateUses: 0,
                fridgeUses: 0,
                claimedQuestMask: 0,
                dailyGiftAvailableAtUnixSeconds: 0,
                adRewardsClaimed: 0,
                finalBonusClaimed: false);
        }
    }
}
