using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoodieMatch.Core.Application.Rewards
{
    public sealed class DailyQuestStatus
    {
        public DailyQuestStatus(
            DailyQuestType type,
            int progress,
            int target,
            int coinReward,
            bool isClaimed)
        {
            Type = type;
            Progress = Math.Min(progress, target);
            Target = target;
            CoinReward = coinReward;
            IsClaimed = isClaimed;
        }

        public DailyQuestType Type { get; }
        public int Progress { get; }
        public int Target { get; }
        public int CoinReward { get; }
        public bool IsClaimed { get; }
        public bool IsCompleted => Progress >= Target;
        public bool CanClaim => IsCompleted && !IsClaimed;
    }

    public sealed class DailyRewardStatus
    {
        public DailyRewardStatus(
            IReadOnlyList<DailyQuestStatus> quests,
            DateTimeOffset dailyGiftAvailableAtUtc,
            DateTimeOffset nowUtc,
            int adRewardsClaimed,
            bool finalBonusClaimed,
            DateTimeOffset resetAtUtc)
        {
            Quests = new ReadOnlyCollection<DailyQuestStatus>(
                new List<DailyQuestStatus>(quests));
            DailyGiftAvailableAtUtc = dailyGiftAvailableAtUtc;
            NowUtc = nowUtc;
            AdRewardsClaimed = adRewardsClaimed;
            FinalBonusClaimed = finalBonusClaimed;
            ResetAtUtc = resetAtUtc;
        }

        public IReadOnlyList<DailyQuestStatus> Quests { get; }
        public DateTimeOffset DailyGiftAvailableAtUtc { get; }
        public DateTimeOffset NowUtc { get; }
        public bool CanClaimDailyGift => DailyGiftAvailableAtUtc <= NowUtc;
        public int AdRewardsClaimed { get; }
        public bool FinalBonusClaimed { get; }
        public DateTimeOffset ResetAtUtc { get; }

        public int ClaimableQuestCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Quests.Count; i++)
                {
                    if (Quests[i].CanClaim)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ClaimableFreeRewardCount =>
            CanClaimDailyGift ||
            AdRewardsClaimed < DailyRewardService.AdRewardCount ||
            !FinalBonusClaimed
                ? 1
                : 0;
    }
}
