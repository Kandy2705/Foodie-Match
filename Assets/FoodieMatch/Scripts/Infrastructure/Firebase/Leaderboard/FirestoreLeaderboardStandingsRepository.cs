using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Firestore;
using FoodieMatch.Core.Application.Authentication;
using FoodieMatch.Core.Application.Leaderboard;

namespace FoodieMatch.Infrastructure.Firebase.Leaderboard
{
    public sealed class FirestoreLeaderboardStandingsRepository :
        ILeaderboardStandingsRepository
    {
        private const int TopPlayerCount = 99;
        private const int OutsideTopRank = TopPlayerCount + 1;
        private const string GlobalCollection = "leaderboardGlobal";
        private const string WeeklyCollection = "leaderboardWeekly";
        private const string WeeklyEntriesCollection = "entries";

        private readonly IPlayerIdentityService _playerIdentityService;
        private FirebaseFirestore _firestore;

        public FirestoreLeaderboardStandingsRepository(
            IPlayerIdentityService playerIdentityService)
        {
            _playerIdentityService = playerIdentityService;
        }

        public Task<LeaderboardStandings> LoadGlobalAsync(
            CancellationToken cancellationToken = default)
        {
            CollectionReference collection = GetFirestore()
                .Collection(GlobalCollection);
            Query query = collection
                .OrderByDescending("highestCompletedLevel")
                .OrderBy("reachedAtUtc")
                .Limit(TopPlayerCount);

            return LoadAsync(
                collection,
                query,
                "highestCompletedLevel",
                cancellationToken);
        }

        public Task<LeaderboardStandings> LoadWeeklyAsync(
            string weekId,
            CancellationToken cancellationToken = default)
        {
            CollectionReference collection = GetFirestore()
                .Collection(WeeklyCollection)
                .Document(weekId)
                .Collection(WeeklyEntriesCollection);
            Query query = collection
                .OrderByDescending("weeklyScore")
                .OrderBy("reachedAtUtc")
                .Limit(TopPlayerCount);

            return LoadAsync(
                collection,
                query,
                "weeklyScore",
                cancellationToken);
        }

        private async Task<LeaderboardStandings> LoadAsync(
            CollectionReference collection,
            Query query,
            string valueField,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_playerIdentityService.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "Player identity is not authenticated.");
            }

            string playerId = _playerIdentityService.PlayerId;
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            cancellationToken.ThrowIfCancellationRequested();

            List<LeaderboardStanding> topPlayers = MapTopPlayers(
                snapshot,
                valueField);
            LeaderboardStanding currentPlayer = topPlayers.Find(
                standing => standing.PlayerId == playerId);

            if (currentPlayer == null)
            {
                DocumentSnapshot currentPlayerSnapshot = await collection
                    .Document(playerId)
                    .GetSnapshotAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (currentPlayerSnapshot.Exists)
                {
                    currentPlayer = MapStanding(
                        currentPlayerSnapshot,
                        valueField,
                        OutsideTopRank);
                }
            }

            return new LeaderboardStandings(topPlayers, currentPlayer);
        }

        private static List<LeaderboardStanding> MapTopPlayers(
            QuerySnapshot snapshot,
            string valueField)
        {
            List<LeaderboardStanding> standings = new(snapshot.Count);
            int rank = 0;
            int previousValue = 0;
            int index = 0;

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                int value = checked((int)document.GetValue<long>(valueField));

                if (index == 0 || value != previousValue)
                {
                    rank = index + 1;
                }

                standings.Add(MapStanding(document, valueField, rank));
                previousValue = value;
                index++;
            }

            return standings;
        }

        private static LeaderboardStanding MapStanding(
            DocumentSnapshot document,
            string valueField,
            int rank)
        {
            return new LeaderboardStanding(
                document.Id,
                document.GetValue<string>("displayName"),
                document.GetValue<string>("avatarId"),
                document.GetValue<string>("frameId"),
                checked((int)document.GetValue<long>(valueField)),
                new DateTimeOffset(
                    document.GetValue<Timestamp>("reachedAtUtc")
                        .ToDateTime()),
                rank);
        }

        private FirebaseFirestore GetFirestore()
        {
            return _firestore ??= FirebaseFirestore.DefaultInstance;
        }
    }
}
