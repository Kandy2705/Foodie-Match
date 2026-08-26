using System;
using System.Globalization;

namespace FoodieMatch.Core.Application.Leaderboard
{
    public static class LeaderboardWeekResolver
    {
        private const int DaysPerWeek = 7;

        public static string GetWeekId(DateTimeOffset utcTime)
        {
            return GetWeekStartUtc(utcTime)
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static DateTimeOffset GetWeekStartUtc(DateTimeOffset utcTime)
        {
            DateTimeOffset normalizedUtc = utcTime.ToUniversalTime();
            int daysSinceMonday =
                ((int)normalizedUtc.DayOfWeek -
                 (int)DayOfWeek.Monday +
                 DaysPerWeek) % DaysPerWeek;

            return new DateTimeOffset(
                    normalizedUtc.Year,
                    normalizedUtc.Month,
                    normalizedUtc.Day,
                    0,
                    0,
                    0,
                    TimeSpan.Zero)
                .AddDays(-daysSinceMonday);
        }
    }
}
