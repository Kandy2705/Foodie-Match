using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Time;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public sealed class LeaderboardStandingsService
    {
        private readonly ILeaderboardStandingsRepository _repository;
        private readonly IClock _clock;

        public LeaderboardStandingsService(
            ILeaderboardStandingsRepository repository,
            IClock clock)
        {
            _repository = repository;
            _clock = clock;
        }

        public Task<LeaderboardStandings> LoadGlobalAsync(
            CancellationToken cancellationToken = default)
        {
            return _repository.LoadGlobalAsync(cancellationToken);
        }

        public Task<LeaderboardStandings> LoadCurrentWeeklyAsync(
            CancellationToken cancellationToken = default)
        {
            return _repository.LoadWeeklyAsync(
                LeaderboardWeekResolver.GetWeekId(_clock.UtcNow),
                cancellationToken);
        }
    }
}
