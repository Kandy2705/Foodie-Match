using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Configuration.GoldPass
{
    public interface IGameGoldPassProgressionConfig
    {
        int UnlockLevel { get; }

        int GetSpoonsPerCompletedLevel(LevelDifficulty difficulty);
    }
}
