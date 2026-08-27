namespace FoodieMatch.Core.Application.Rewards
{
    public interface IDailyRewardProgressStore
    {
        DailyRewardProgress Load();

        void Save(DailyRewardProgress progress);
    }
}
