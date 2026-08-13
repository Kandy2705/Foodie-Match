using System;

namespace FoodieMatch.Core.Application.GoldPass
{
    public sealed class GoldPassSeasonPeriod
    {
        public GoldPassSeasonPeriod(
            string seasonId,
            DateTimeOffset startUtc,
            DateTimeOffset endUtc)
        {
            SeasonId = seasonId ?? throw new ArgumentNullException(nameof(seasonId));
            StartUtc = startUtc;
            EndUtc = endUtc;
        }

        public string SeasonId { get; }

        public DateTimeOffset StartUtc { get; }

        public DateTimeOffset EndUtc { get; }
    }
}
