using System;

namespace FoodieMatch.Core.Application.Time
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
