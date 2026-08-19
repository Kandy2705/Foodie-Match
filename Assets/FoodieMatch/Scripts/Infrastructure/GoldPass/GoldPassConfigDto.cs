namespace FoodieMatch.Infrastructure.GoldPass
{
    internal sealed class GoldPassConfigDto
    {
        public GoldPassPurchaseDto purchase;
        public string resetDayUtc;
        public int resetHourUtc;
        public GoldPassMilestoneDto[] milestones;
    }

    internal sealed class GoldPassPurchaseDto
    {
        public string storeProductId;
        public string fallbackDisplayPrice;
    }

    internal sealed class GoldPassMilestoneDto
    {
        public int level;
        public int requiredSpoons;
        public GoldPassRewardDto freeReward;
        public GoldPassRewardDto seasonReward;
    }

    internal sealed class GoldPassRewardDto
    {
        public string type;
        public long amount;
        public long durationMinutes;
        public string boosterType;
        public GoldPassRewardDto[] contents;
    }
}
