using System;
using FoodieMatch.Core.Domain.Player;

namespace FoodieMatch.Core.Application.Player
{
    public sealed class PlayerProfileRecord
    {
        public PlayerProfileRecord(PlayerProfile profile, long revision)
        {
            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revision),
                    revision,
                    "Revision cannot be negative.");
            }

            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Revision = revision;
        }

        public PlayerProfile Profile { get; }

        public long Revision { get; }
    }
}
