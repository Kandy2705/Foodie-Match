using System;

namespace FoodieMatch.Core.Application.Configuration.Advertising
{
    public interface IGameAdsConfig
    {
        TimeSpan PostLevelAdInterval { get; }
    }
}
