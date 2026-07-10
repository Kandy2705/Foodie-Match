using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Repositories
{
    public interface ILevelRepository
    {
        bool TryGetLevel(
            int levelNumber,
            out LevelConfig levelConfig);

        bool TryGetFirstLevel(out LevelConfig levelConfig);

        bool TryGetNextLevel(
            int currentLevelNumber,
            out LevelConfig levelConfig);
    }
}
