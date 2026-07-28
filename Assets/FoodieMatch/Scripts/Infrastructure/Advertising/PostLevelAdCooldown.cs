using System;
using System.Globalization;
using FoodieMatch.Core.Application.Time;
using FoodieMatch.Infrastructure.Persistence.Save;

namespace FoodieMatch.Infrastructure.Advertising
{
    public sealed class PostLevelAdCooldown
    {
        private const string CooldownStartedAtUtcKey =
            "Advertising.CooldownStartedAtUtc";

        private readonly ISaveService _saveService;
        private readonly IClock _clock;

        public PostLevelAdCooldown(ISaveService saveService, IClock clock)
        {
            _saveService = saveService;
            _clock = clock;

            if (!_saveService.HasKey(CooldownStartedAtUtcKey))
            {
                Restart();
            }
        }

        public bool HasElapsed(TimeSpan interval)
        {
            string savedValue = _saveService.GetString(
                CooldownStartedAtUtcKey,
                defaultValue: string.Empty);

            if (!long.TryParse(
                    savedValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out long unixTimeSeconds))
            {
                Restart();
                return false;
            }

            DateTimeOffset cooldownStartedAt =
                DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
            return _clock.UtcNow - cooldownStartedAt >= interval;
        }

        public void Restart()
        {
            string unixTimeSeconds = _clock.UtcNow
                .ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture);
            _saveService.SetString(
                CooldownStartedAtUtcKey,
                unixTimeSeconds);
            _saveService.Save();
        }
    }
}
