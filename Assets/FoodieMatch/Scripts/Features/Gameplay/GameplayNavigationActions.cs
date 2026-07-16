using System;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class GameplayNavigationActions
    {
        public GameplayNavigationActions(
            Action homeRequested,
            Action<int> retryRequested,
            Action<int> winRewardClaimed)
        {
            HomeRequested = homeRequested ?? throw new ArgumentNullException(nameof(homeRequested));
            RetryRequested = retryRequested ?? throw new ArgumentNullException(nameof(retryRequested));
            WinRewardClaimed = winRewardClaimed ?? throw new ArgumentNullException(nameof(winRewardClaimed));
        }

        public Action HomeRequested { get; }
        public Action<int> RetryRequested { get; }
        public Action<int> WinRewardClaimed { get; }
    }
}
