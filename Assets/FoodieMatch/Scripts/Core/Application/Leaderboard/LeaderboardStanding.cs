using System;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public sealed class LeaderboardStanding
    {
        public LeaderboardStanding(
            string playerId,
            string displayName,
            string avatarId,
            string frameId,
            int value,
            DateTimeOffset reachedAtUtc,
            int rank)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            AvatarId = avatarId;
            FrameId = frameId;
            Value = value;
            ReachedAtUtc = reachedAtUtc.ToUniversalTime();
            Rank = rank;
        }

        public string PlayerId { get; }

        public string DisplayName { get; }

        public string AvatarId { get; }

        public string FrameId { get; }

        public int Value { get; }

        public DateTimeOffset ReachedAtUtc { get; }

        public int Rank { get; }
    }
}
