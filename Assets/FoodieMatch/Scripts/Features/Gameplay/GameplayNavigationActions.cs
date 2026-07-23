using System;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayNavigationActions
    {
        public GameplayNavigationActions(
            Action homeRequested,
            Action<int> retryRequested,
            Action<int> levelWon)
        {
            HomeRequested = homeRequested ?? throw new ArgumentNullException(nameof(homeRequested));
            RetryRequested = retryRequested ?? throw new ArgumentNullException(nameof(retryRequested));
            LevelWon = levelWon ?? throw new ArgumentNullException(nameof(levelWon));
        }

        public Action HomeRequested { get; }
        public Action<int> RetryRequested { get; }
        public Action<int> LevelWon { get; }
    }
}
