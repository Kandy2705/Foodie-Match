using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public interface IGameGoldPassProgressionConfig
    {
        int GetSpoonsPerCompletedLevel(LevelDifficulty difficulty);
    }
}
