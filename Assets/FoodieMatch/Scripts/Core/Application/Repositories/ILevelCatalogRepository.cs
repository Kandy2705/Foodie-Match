using FoodieMatch.Core.Domain.Level;

namespace FoodieMatch.Core.Application.Repositories
{
    public interface ILevelCatalogRepository
    {
        bool TryGetLevelSummary(
            int levelNumber,
            out LevelSummary summary);

        bool TryGetFirstLevelSummary(
            out LevelSummary summary);

        bool TryGetNextLevelSummary(
            int currentLevelNumber,
            out LevelSummary summary);
    }
}
