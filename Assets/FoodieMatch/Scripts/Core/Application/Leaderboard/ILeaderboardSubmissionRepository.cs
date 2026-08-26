using System.Threading;
using System.Threading.Tasks;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public interface ILeaderboardSubmissionRepository
    {
        Task<bool> TrySubmitAsync(
            LeaderboardCompletion completion,
            bool countsTowardWeekly,
            CancellationToken cancellationToken = default);
    }
}
