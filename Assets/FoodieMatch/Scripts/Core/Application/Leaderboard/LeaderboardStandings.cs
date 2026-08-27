using System.Collections.Generic;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public sealed class LeaderboardStandings
    {
        public LeaderboardStandings(
            IReadOnlyList<LeaderboardStanding> topPlayers,
            LeaderboardStanding currentPlayer)
        {
            TopPlayers = topPlayers;
            CurrentPlayer = currentPlayer;
        }

        public IReadOnlyList<LeaderboardStanding> TopPlayers { get; }

        public LeaderboardStanding CurrentPlayer { get; }
    }
}
