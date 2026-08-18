using System;
using System.Globalization;
using FoodieMatch.Core.Application.Configuration.GoldPass;

namespace FoodieMatch.Core.Application.GoldPass
{
    public sealed class GoldPassSeasonSchedule
    {
        private const int DaysPerWeek = 7;
        private readonly IGameGoldPassConfig _config;

        public GoldPassSeasonSchedule(IGameGoldPassConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public GoldPassSeasonPeriod GetCurrentSeason(DateTimeOffset utcNow)
        {
            DateTimeOffset normalizedUtc = utcNow.ToUniversalTime();
            int daysSinceReset =
                ((int)normalizedUtc.DayOfWeek -
                 (int)_config.ResetDayUtc +
                 DaysPerWeek) % DaysPerWeek;
            DateTimeOffset startUtc = new(
                normalizedUtc.Year,
                normalizedUtc.Month,
                normalizedUtc.Day,
                _config.ResetHourUtc,
                0,
                0,
                TimeSpan.Zero);
            startUtc = startUtc.AddDays(-daysSinceReset);

            if (startUtc > normalizedUtc)
            {
                startUtc = startUtc.AddDays(-DaysPerWeek);
            }

            return new GoldPassSeasonPeriod(
                $"weekly_{startUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}",
                startUtc,
                startUtc.AddDays(DaysPerWeek));
        }
    }
}
