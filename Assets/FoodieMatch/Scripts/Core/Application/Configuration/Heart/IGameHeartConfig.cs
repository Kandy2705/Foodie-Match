using System;

namespace FoodieMatch.Core.Application.Configuration.Heart
{
    public interface IGameHeartConfig
    {
        int MaxHeartCount { get; }

        TimeSpan HeartRecoveryDuration { get; }
    }
}
