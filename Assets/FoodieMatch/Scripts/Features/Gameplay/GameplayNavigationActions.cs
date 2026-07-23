using System;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayNavigationActions
    {
        public GameplayNavigationActions(
            Action homeRequested,
            Action<int> retryRequested,
            Action<int> levelLost,
            Action<int> levelWon)
        {
            HomeRequested = homeRequested ??
                throw new ArgumentNullException(nameof(homeRequested));
            RetryRequested = retryRequested ??
                throw new ArgumentNullException(nameof(retryRequested));
            LevelLost = levelLost ??
                throw new ArgumentNullException(nameof(levelLost));
            LevelWon = levelWon ??
                throw new ArgumentNullException(nameof(levelWon));
        }

        public Action HomeRequested { get; }

        public Action<int> RetryRequested { get; }

        public Action<int> LevelLost { get; }

        public Action<int> LevelWon { get; }
    }
}
