using System.Collections.Generic;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public interface ILeaderboardPendingSubmissionStore
    {
        bool TryLoad(out IReadOnlyList<LeaderboardCompletion> completions);

        bool TrySave(IReadOnlyList<LeaderboardCompletion> completions);
    }
}
