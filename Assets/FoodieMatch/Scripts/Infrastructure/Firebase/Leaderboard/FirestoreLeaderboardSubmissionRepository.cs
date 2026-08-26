using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Firestore;
using FoodieMatch.Core.Application.Authentication;
using FoodieMatch.Core.Application.Leaderboard;

namespace FoodieMatch.Infrastructure.Firebase.Leaderboard
{
    public sealed class FirestoreLeaderboardSubmissionRepository :
        ILeaderboardSubmissionRepository
    {
        private const string GlobalCollection = "leaderboardGlobal";
        private const string WeeklyCollection = "leaderboardWeekly";
        private const string WeeklyEntriesCollection = "entries";
        private const string CompletionsCollection = "leaderboardCompletions";
        private const string CompletionEventsCollection = "events";

        private readonly IPlayerIdentityService _playerIdentityService;
        private FirebaseFirestore _firestore;

        public FirestoreLeaderboardSubmissionRepository(
            IPlayerIdentityService playerIdentityService)
        {
            _playerIdentityService = playerIdentityService;
        }

        public async Task<bool> TrySubmitAsync(
            LeaderboardCompletion completion,
            bool countsTowardWeekly,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_playerIdentityService.IsAuthenticated)
            {
                return false;
            }

            try
            {
                string playerId = _playerIdentityService.PlayerId;
                FirebaseFirestore firestore = GetFirestore();
                DocumentReference globalReference = firestore
                    .Collection(GlobalCollection)
                    .Document(playerId);
                DocumentReference completionReference = firestore
                    .Collection(CompletionsCollection)
                    .Document(playerId)
                    .Collection(CompletionEventsCollection)
                    .Document(completion.CompletionId);
                DocumentReference weeklyReference = firestore
                    .Collection(WeeklyCollection)
                    .Document(completion.WeekId)
                    .Collection(WeeklyEntriesCollection)
                    .Document(playerId);

                await firestore.RunTransactionAsync<bool>(
                    async transaction =>
                    {
                        DocumentSnapshot completionSnapshot =
                            await transaction.GetSnapshotAsync(
                                completionReference);

                        if (completionSnapshot.Exists)
                        {
                            return true;
                        }

                        DocumentSnapshot globalSnapshot =
                            await transaction.GetSnapshotAsync(globalReference);
                        DocumentSnapshot weeklySnapshot = countsTowardWeekly
                            ? await transaction.GetSnapshotAsync(weeklyReference)
                            : null;
                        long completedLevelNumber = completion.LevelNumber;

                        if (globalSnapshot.Exists)
                        {
                            long currentLevelNumber = ReadLong(
                                globalSnapshot,
                                "highestCompletedLevel");

                            if (completedLevelNumber != currentLevelNumber + 1)
                            {
                                throw new InvalidOperationException(
                                    "Leaderboard levels must be submitted in order.");
                            }
                        }

                        Timestamp completedAt = Timestamp.FromDateTime(
                            completion.CompletedAtUtc.UtcDateTime);
                        transaction.Set(
                            completionReference,
                            CreateCompletionData(
                                completion,
                                completedAt,
                                countsTowardWeekly));
                        transaction.Set(
                            globalReference,
                            CreateGlobalData(
                                playerId,
                                completion,
                                completedAt));

                        if (countsTowardWeekly)
                        {
                            long weeklyScore = weeklySnapshot.Exists
                                ? checked(ReadLong(
                                    weeklySnapshot,
                                    "weeklyScore") + 1)
                                : 1;
                            transaction.Set(
                                weeklyReference,
                                CreateWeeklyData(
                                    playerId,
                                    completion,
                                    completedAt,
                                    weeklyScore));
                        }

                        return true;
                    });
                cancellationToken.ThrowIfCancellationRequested();
                return true;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Dictionary<string, object> CreateCompletionData(
            LeaderboardCompletion completion,
            Timestamp completedAt,
            bool countsTowardWeekly)
        {
            return new Dictionary<string, object>
            {
                ["levelNumber"] = (long)completion.LevelNumber,
                ["weekId"] = completion.WeekId,
                ["completedAtUtc"] = completedAt,
                ["countsTowardWeekly"] = countsTowardWeekly,
                ["createdAt"] = FieldValue.ServerTimestamp
            };
        }

        private static Dictionary<string, object> CreateGlobalData(
            string playerId,
            LeaderboardCompletion completion,
            Timestamp completedAt)
        {
            return new Dictionary<string, object>
            {
                ["playerId"] = playerId,
                ["displayName"] = completion.PlayerName,
                ["avatarId"] = completion.AvatarId,
                ["frameId"] = completion.FrameId,
                ["highestCompletedLevel"] = (long)completion.LevelNumber,
                ["reachedAtUtc"] = completedAt,
                ["updatedAt"] = FieldValue.ServerTimestamp,
                ["lastCompletionId"] = completion.CompletionId
            };
        }

        private static Dictionary<string, object> CreateWeeklyData(
            string playerId,
            LeaderboardCompletion completion,
            Timestamp completedAt,
            long weeklyScore)
        {
            return new Dictionary<string, object>
            {
                ["playerId"] = playerId,
                ["displayName"] = completion.PlayerName,
                ["avatarId"] = completion.AvatarId,
                ["frameId"] = completion.FrameId,
                ["weekId"] = completion.WeekId,
                ["weeklyScore"] = weeklyScore,
                ["reachedAtUtc"] = completedAt,
                ["updatedAt"] = FieldValue.ServerTimestamp,
                ["lastCompletionId"] = completion.CompletionId
            };
        }

        private static long ReadLong(
            DocumentSnapshot snapshot,
            string fieldName)
        {
            if (snapshot.TryGetValue(fieldName, out long value))
            {
                return value;
            }

            throw new InvalidOperationException(
                $"Leaderboard field '{fieldName}' is invalid.");
        }

        private FirebaseFirestore GetFirestore()
        {
            return _firestore ??= FirebaseFirestore.DefaultInstance;
        }
    }
}
