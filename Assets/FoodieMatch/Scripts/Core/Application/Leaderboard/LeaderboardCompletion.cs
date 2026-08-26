using System;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public sealed class LeaderboardCompletion
    {
        public LeaderboardCompletion(
            string completionId,
            int levelNumber,
            DateTimeOffset completedAtUtc,
            string weekId,
            string playerName,
            string avatarId,
            string frameId)
        {
            CompletionId = completionId;
            LevelNumber = levelNumber;
            CompletedAtUtc = completedAtUtc.ToUniversalTime();
            WeekId = weekId;
            PlayerName = playerName;
            AvatarId = avatarId;
            FrameId = frameId;
        }

        public string CompletionId { get; }

        public int LevelNumber { get; }

        public DateTimeOffset CompletedAtUtc { get; }

        public string WeekId { get; }

        public string PlayerName { get; }

        public string AvatarId { get; }

        public string FrameId { get; }
    }
}
