using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoodieMatch.Core.Application.Level
{
    public interface ILevelSynchronizer
    {
        event Action CatalogUpdated;

        bool IsLevelAvailable(int levelNumber);

        Task<bool> EnsureLevelAvailableAsync(
            int levelNumber,
            CancellationToken cancellationToken);

        Task SynchronizeUpcomingLevelsAsync(
            int currentLevelNumber,
            int followingLevelCount,
            CancellationToken cancellationToken);
    }
}
