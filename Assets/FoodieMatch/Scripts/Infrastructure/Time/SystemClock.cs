using System;
using FoodieMatch.Core.Application.Time;

namespace FoodieMatch.Infrastructure.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
