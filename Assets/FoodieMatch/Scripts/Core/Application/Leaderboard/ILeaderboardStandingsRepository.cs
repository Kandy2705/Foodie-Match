using System.Threading;
using System.Threading.Tasks;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public interface ILeaderboardStandingsRepository
    {
        Task<LeaderboardStandings> LoadGlobalAsync(
            CancellationToken cancellationToken = default);

        Task<LeaderboardStandings> LoadWeeklyAsync(
            string weekId,
            CancellationToken cancellationToken = default);
    }
}
