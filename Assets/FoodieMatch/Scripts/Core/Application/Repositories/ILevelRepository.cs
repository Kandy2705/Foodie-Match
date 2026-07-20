using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Repositories
{
    public interface ILevelRepository
    {
        bool TryGetLevel(int levelNumber, out LevelDefinition level);

        bool TryGetFirstLevel(out LevelDefinition level);

        bool TryGetNextLevel(int currentLevelNumber, out LevelDefinition level);
    }
}
