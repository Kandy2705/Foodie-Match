using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Time;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public sealed class LeaderboardSubmissionService
    {
        private const int WeeklySubmissionAgeDays = 7;

        private readonly object _pendingLock = new();
        private readonly SemaphoreSlim _flushLock = new(1, 1);
        private readonly ILeaderboardSubmissionRepository _repository;
        private readonly ILeaderboardPendingSubmissionStore _pendingStore;
        private readonly IClock _clock;
        private readonly List<LeaderboardCompletion> _pendingCompletions;

        public LeaderboardSubmissionService(
            ILeaderboardSubmissionRepository repository,
            ILeaderboardPendingSubmissionStore pendingStore,
            IClock clock)
        {
            _repository = repository;
            _pendingStore = pendingStore;
            _clock = clock;
            _pendingCompletions = pendingStore.TryLoad(
                out IReadOnlyList<LeaderboardCompletion> completions)
                ? new List<LeaderboardCompletion>(completions)
                : new List<LeaderboardCompletion>();
        }

        public void QueueLevelCompletion(
            int levelNumber,
            string playerName,
            string avatarId,
            string frameId)
        {
            string completionId = $"level_{levelNumber:D4}";
            DateTimeOffset completedAtUtc = _clock.UtcNow;
            LeaderboardCompletion completion = new(
                completionId,
                levelNumber,
                completedAtUtc,
                LeaderboardWeekResolver.GetWeekId(completedAtUtc),
                playerName,
                avatarId,
                frameId);

            lock (_pendingLock)
            {
                if (_pendingCompletions.Any(
                        pending => pending.CompletionId == completionId))
                {
                    return;
                }

                _pendingCompletions.Add(completion);
                _pendingStore.TrySave(_pendingCompletions);
            }

            _ = FlushPendingAsync();
        }

        public async Task FlushPendingAsync(
            CancellationToken cancellationToken = default)
        {
            await _flushLock.WaitAsync(cancellationToken);

            try
            {
                while (TryGetNextPending(out LeaderboardCompletion completion))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bool countsTowardWeekly =
                        completion.CompletedAtUtc >=
                        _clock.UtcNow.AddDays(-WeeklySubmissionAgeDays);
                    bool submitted = await _repository.TrySubmitAsync(
                        completion,
                        countsTowardWeekly,
                        cancellationToken);

                    if (!submitted)
                    {
                        return;
                    }

                    RemovePending(completion.CompletionId);
                }
            }
            finally
            {
                _flushLock.Release();
            }
        }

        private bool TryGetNextPending(out LeaderboardCompletion completion)
        {
            lock (_pendingLock)
            {
                completion = _pendingCompletions.Count > 0
                    ? _pendingCompletions[0]
                    : null;
                return completion != null;
            }
        }

        private void RemovePending(string completionId)
        {
            lock (_pendingLock)
            {
                _pendingCompletions.RemoveAll(
                    completion => completion.CompletionId == completionId);
                _pendingStore.TrySave(_pendingCompletions);
            }
        }
    }
}
